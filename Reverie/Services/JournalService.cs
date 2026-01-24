using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Reverie.Models;

namespace Reverie.Services;

public class JournalService
{
    private readonly string _filePath;
    private readonly Dictionary<DateTime, JournalEntry> _entries = new();

    // Optional: default tags that appear even if user has no history
    private static readonly string[] DefaultTags =
    [
        "Work","Health","Meditation","Travel","Family","Fitness","Personal","Career","Studies"
    ];

    public JournalService()
    {
        // MAUI-friendly app data directory
        var dir = FileSystem.AppDataDirectory;
        _filePath = Path.Combine(dir, "journal.entries.json");
        LoadFromDisk();
    }

    public JournalEntry? GetByDate(DateTime date)
    {
        _entries.TryGetValue(date.Date, out var entry);
        return entry;
    }

    public IEnumerable<JournalEntry> GetAll()
        => _entries.Values.OrderByDescending(e => e.Date);

    // Useful for Calendar / Timeline
    public IEnumerable<JournalEntry> GetRange(DateTime fromInclusive, DateTime toInclusive)
        => _entries.Values
            .Where(e => e.Date.Date >= fromInclusive.Date && e.Date.Date <= toInclusive.Date)
            .OrderByDescending(e => e.Date);

    // NEW: Get paginated entries
    public (IEnumerable<JournalEntry> Entries, int TotalPages, int TotalCount) GetPaginated(int pageNumber, int pageSize = 10)
    {
        var allEntries = _entries.Values.OrderByDescending(e => e.Date).ToList();
        var totalCount = allEntries.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var entries = allEntries
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (entries, totalPages, totalCount);
    }

    // Dynamic suggestions: defaults + tags you've actually used before
    public IReadOnlyList<string> GetSuggestedTags(int max = 24)
    {
        var fromEntries = _entries.Values
            .SelectMany(e => e.Tags ?? new List<string>())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(NormalizeTag)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // prioritize frequently used tags
        var freq = _entries.Values
            .SelectMany(e => e.Tags ?? new List<string>())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(NormalizeTag)
            .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
            .Select(g => new { Tag = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Tag)
            .Select(x => x.Tag)
            .ToList();

        var merged = DefaultTags
            .Concat(freq)
            .Concat(fromEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(max)
            .ToList();

        return merged;
    }

    public void Save(JournalEntry entry)
    {
        if (entry is null) throw new ArgumentNullException(nameof(entry));

        // Normalize key + content
        entry.Date = entry.Date.Date;
        entry.Title = (entry.Title ?? "").Trim();
        entry.Content = entry.Content ?? "";
        entry.PrimaryMood = (entry.PrimaryMood ?? "").Trim();

        entry.Tags = (entry.Tags ?? new List<string>())
            .Select(NormalizeTag)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t)
            .ToList();

        entry.SecondaryMoods = (entry.SecondaryMoods ?? new List<string>())
            .Select(m => (m ?? "").Trim())
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(2)
            .ToList();

        // Keep CreatedAt if it already exists
        if (_entries.TryGetValue(entry.Date, out var existing))
        {
            entry.CreatedAt = existing.CreatedAt == default ? DateTime.Now : existing.CreatedAt;
        }
        else
        {
            entry.CreatedAt = DateTime.Now;
        }

        entry.UpdatedAt = DateTime.Now;

        _entries[entry.Date] = entry;

        SaveToDisk();
    }

    public void Delete(DateTime date)
    {
        if (_entries.Remove(date.Date))
            SaveToDisk();
    }

    public int GetCurrentStreak()
    {
        int streak = 0;
        var date = DateTime.Today.Date;
        while (_entries.ContainsKey(date))
        {
            streak++;
            date = date.AddDays(-1);
        }
        return streak;
    }

    // NEW: Get all unique entry dates
    public HashSet<DateTime> GetEntryDates()
    {
        return _entries.Keys.ToHashSet();
    }

    // NEW: Get entries within a date range
    public List<JournalEntry> GetEntriesInRange(DateTime startDate, DateTime endDate)
    {
        return _entries.Values
            .Where(e => e.Date.Date >= startDate.Date && e.Date.Date <= endDate.Date)
            .OrderByDescending(e => e.Date)
            .ToList();
    }

    // NEW: Get all unique tags with counts
    public Dictionary<string, int> GetAllTagsWithCounts()
    {
        var tagCounts = new Dictionary<string, int>();

        foreach (var entry in _entries.Values)
        {
            if (entry.Tags != null)
            {
                foreach (var tag in entry.Tags)
                {
                    if (!string.IsNullOrWhiteSpace(tag))
                    {
                        var normalizedTag = NormalizeTag(tag);
                        if (tagCounts.ContainsKey(normalizedTag))
                            tagCounts[normalizedTag]++;
                        else
                            tagCounts[normalizedTag] = 1;
                    }
                }
            }
        }

        return tagCounts;
    }

    // NEW: Get word count for an entry
    public int GetWordCount(JournalEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.Content))
            return 0;

        return entry.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }

    // NEW: Categorize mood into positive/neutral/negative
    public string CategorizeMood(string mood)
    {
        if (string.IsNullOrWhiteSpace(mood))
            return "neutral";

        mood = mood.ToLower();

        var positiveKeywords = new[] {
            "happy", "joy", "excited", "calm", "peace", "content",
            "energetic", "proud", "grateful", "love", "optimistic", "relaxed"
        };

        var negativeKeywords = new[] {
            "sad", "angry", "anxious", "stressed", "tired", "frustrated",
            "overwhelmed", "lonely", "depressed", "upset", "worried", "exhausted"
        };

        if (positiveKeywords.Any(keyword => mood.Contains(keyword)))
            return "positive";
        else if (negativeKeywords.Any(keyword => mood.Contains(keyword)))
            return "negative";
        else
            return "neutral";
    }

    // NEW: Get mood distribution
    public (int Positive, int Neutral, int Negative) GetMoodDistribution(List<JournalEntry> entries = null)
    {
        var targetEntries = entries ?? _entries.Values.ToList();

        int positive = 0, neutral = 0, negative = 0;

        foreach (var entry in targetEntries)
        {
            var category = CategorizeMood(entry.PrimaryMood);

            switch (category)
            {
                case "positive": positive++; break;
                case "neutral": neutral++; break;
                case "negative": negative++; break;
            }
        }

        return (positive, neutral, negative);
    }

    // NEW: Get top N tags
    public List<(string Tag, int Count)> GetTopTags(int count = 5)
    {
        var tagCounts = GetAllTagsWithCounts();

        return tagCounts
            .OrderByDescending(kv => kv.Value)
            .Take(count)
            .Select(kv => (kv.Key, kv.Value))
            .ToList();
    }

    // NEW: Calculate longest streak
    public int CalculateLongestStreak()
    {
        if (!_entries.Any())
            return 0;

        var dates = _entries.Keys
            .OrderBy(d => d)
            .ToList();

        if (!dates.Any())
            return 0;

        int longestStreak = 1;
        int currentStreak = 1;

        for (int i = 1; i < dates.Count; i++)
        {
            var diff = (dates[i] - dates[i - 1]).Days;

            if (diff == 1)
            {
                // Consecutive day
                currentStreak++;
                longestStreak = Math.Max(longestStreak, currentStreak);
            }
            else if (diff > 1)
            {
                // Gap in entries
                currentStreak = 1;
            }
        }

        return longestStreak;
    }

    // NEW: Calculate missed days in a range
    public int CalculateMissedDays(DateTime startDate, DateTime endDate)
    {
        var entryDates = GetEntryDates();

        int missedDays = 0;
        var currentDate = startDate.Date;

        while (currentDate <= endDate.Date)
        {
            if (!entryDates.Contains(currentDate))
            {
                missedDays++;
            }
            currentDate = currentDate.AddDays(1);
        }

        return missedDays;
    }

    // NEW: Get word count trends
    public List<int> GetWordCountTrends(DateTime startDate, DateTime endDate, int segments = 8)
    {
        var result = new List<int>();
        var entriesInRange = GetEntriesInRange(startDate, endDate);

        if (!entriesInRange.Any())
        {
            // Return zeros if no data
            return Enumerable.Repeat(0, segments).ToList();
        }

        // Group entries by time segments (weeks, days, etc.)
        var totalDays = (int)(endDate - startDate).TotalDays + 1;
        var daysPerSegment = Math.Max(1, totalDays / segments);

        for (int segment = 0; segment < segments; segment++)
        {
            var segmentStart = startDate.AddDays(segment * daysPerSegment);
            var segmentEnd = (segment == segments - 1) ?
                endDate : segmentStart.AddDays(daysPerSegment - 1);

            var segmentEntries = entriesInRange
                .Where(e => e.Date >= segmentStart && e.Date <= segmentEnd)
                .ToList();

            if (segmentEntries.Any())
            {
                var avgWords = (int)segmentEntries.Average(e => GetWordCount(e));
                result.Add(avgWords);
            }
            else
            {
                result.Add(0);
            }
        }

        return result;
    }

    private static string NormalizeTag(string tag)
    {
        tag = (tag ?? "").Trim();
        if (tag.Length == 0) return "";

        // Title-case-ish but preserves acronyms reasonably
        // e.g. "work" -> "Work", "mental health" -> "Mental Health"
        var words = tag.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', words.Select(w =>
        {
            if (w.Length == 1) return w.ToUpperInvariant();
            return char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant();
        }));
    }

    private void LoadFromDisk()
    {
        try
        {
            if (!File.Exists(_filePath)) return;

            var json = File.ReadAllText(_filePath);
            var list = JsonSerializer.Deserialize<List<JournalEntry>>(json) ?? new();

            _entries.Clear();
            foreach (var e in list)
            {
                if (e is null) continue;
                e.Date = e.Date.Date;
                _entries[e.Date] = e;
            }
        }
        catch
        {
            // If file is corrupt, don't crash the app. Start fresh.
            _entries.Clear();
        }
    }

    private void SaveToDisk()
    {
        var list = _entries.Values.OrderBy(e => e.Date).ToList();
        var json = JsonSerializer.Serialize(list, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        File.WriteAllText(_filePath, json);
    }
}