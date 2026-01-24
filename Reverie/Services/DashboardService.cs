using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reverie.Models;

namespace Reverie.Services;

public class DashboardService
{
    private readonly JournalService _journalService;

    public DashboardService(JournalService journalService)
    {
        _journalService = journalService;
    }

    public DashboardSummary GetSummary(string range = "Last 30 days")
    {
        var entries = _journalService.GetAll().ToList();
        var dateRange = GetDateRange(range);
        var filteredEntries = entries
            .Where(e => e.Date >= dateRange.Start && e.Date <= dateRange.End)
            .ToList();

        return new DashboardSummary
        {
            CurrentStreakDays = CalculateCurrentStreak(entries),
            LongestStreakDays = CalculateLongestStreak(entries),
            MissedDays = CalculateMissedDays(filteredEntries, dateRange),
            PositivePercent = CalculateMoodPercentage(filteredEntries, "positive"),
            NeutralPercent = CalculateMoodPercentage(filteredEntries, "neutral"),
            NegativePercent = CalculateMoodPercentage(filteredEntries, "negative"),
            TopTags = GetTopTags(filteredEntries, 4),
            WordCounts = GetWordCountTrends(filteredEntries, dateRange)
        };
    }

    private (DateTime Start, DateTime End) GetDateRange(string range)
    {
        var today = DateTime.Today;

        return range switch
        {
            "Last 7 days" => (today.AddDays(-6), today),
            "Last 90 days" => (today.AddDays(-89), today),
            _ => (today.AddDays(-29), today) // Default: Last 30 days
        };
    }

    private int CalculateCurrentStreak(List<JournalEntry> entries)
    {
        int streak = 0;
        var date = DateTime.Today;

        // Check if today has an entry
        var hasTodayEntry = entries.Any(e => e.Date.Date == date);
        if (!hasTodayEntry)
        {
            date = date.AddDays(-1);
        }

        // Count consecutive days with entries
        while (entries.Any(e => e.Date.Date == date))
        {
            streak++;
            date = date.AddDays(-1);
        }

        return streak;
    }

    private int CalculateLongestStreak(List<JournalEntry> entries)
    {
        if (!entries.Any()) return 0;

        var dates = entries
            .Select(e => e.Date.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        if (!dates.Any()) return 0;

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

    private int CalculateMissedDays(List<JournalEntry> entries, (DateTime Start, DateTime End) range)
    {
        var entryDates = entries
            .Select(e => e.Date.Date)
            .Distinct()
            .ToHashSet();

        int missedDays = 0;
        var currentDate = range.Start;

        while (currentDate <= range.End)
        {
            if (!entryDates.Contains(currentDate))
            {
                missedDays++;
            }
            currentDate = currentDate.AddDays(1);
        }

        return missedDays;
    }

    private int CalculateMoodPercentage(List<JournalEntry> entries, string moodCategory)
    {
        if (!entries.Any()) return 0;

        var moodCounts = new Dictionary<string, int>
        {
            ["positive"] = 0,
            ["neutral"] = 0,
            ["negative"] = 0
        };

        // Categorize each entry's primary mood
        foreach (var entry in entries)
        {
            var mood = entry.PrimaryMood?.ToLower() ?? "";

            if (mood.Contains("happy") || mood.Contains("joy") || mood.Contains("excited") ||
                mood.Contains("calm") || mood.Contains("peace") || mood.Contains("content"))
            {
                moodCounts["positive"]++;
            }
            else if (mood.Contains("sad") || mood.Contains("angry") || mood.Contains("anxious") ||
                    mood.Contains("stressed") || mood.Contains("tired") || mood.Contains("frustrated"))
            {
                moodCounts["negative"]++;
            }
            else if (!string.IsNullOrWhiteSpace(mood))
            {
                moodCounts["neutral"]++;
            }
            else
            {
                // No mood specified, count as neutral
                moodCounts["neutral"]++;
            }
        }

        var totalEntries = entries.Count;
        var count = moodCounts.GetValueOrDefault(moodCategory, 0);

        return totalEntries > 0 ? (int)Math.Round((count * 100.0) / totalEntries) : 0;
    }

    private List<TagUsage> GetTopTags(List<JournalEntry> entries, int count)
    {
        if (!entries.Any())
        {
            // Return default tags if no entries
            return new List<TagUsage>
            {
                new() { Name = "Work", Count = 0, ColorHex = "#6C2BD9" },
                new() { Name = "Health", Count = 0, ColorHex = "#2F6BFF" },
                new() { Name = "Travel", Count = 0, ColorHex = "#EC4899" },
                new() { Name = "Fitness", Count = 0, ColorHex = "#16A34A" }
            };
        }

        // Count all tags from entries
        var tagCounts = new Dictionary<string, int>();

        foreach (var entry in entries)
        {
            if (entry.Tags != null)
            {
                foreach (var tag in entry.Tags)
                {
                    if (!string.IsNullOrWhiteSpace(tag))
                    {
                        var normalizedTag = char.ToUpper(tag[0]) + tag[1..].ToLower();
                        if (tagCounts.ContainsKey(normalizedTag))
                            tagCounts[normalizedTag]++;
                        else
                            tagCounts[normalizedTag] = 1;
                    }
                }
            }
        }

        // Get top N tags
        var topTags = tagCounts
            .OrderByDescending(kv => kv.Value)
            .Take(count)
            .Select((kv, index) => new TagUsage
            {
                Name = kv.Key,
                Count = kv.Value,
                ColorHex = GetTagColor(index)
            })
            .ToList();

        // Fill with default tags if we don't have enough
        var defaultTags = new List<TagUsage>
        {
            new() { Name = "Work", Count = 0, ColorHex = "#6C2BD9" },
            new() { Name = "Health", Count = 0, ColorHex = "#2F6BFF" },
            new() { Name = "Travel", Count = 0, ColorHex = "#EC4899" },
            new() { Name = "Fitness", Count = 0, ColorHex = "#16A34A" }
        };

        while (topTags.Count < count)
        {
            var defaultTag = defaultTags[topTags.Count];
            defaultTag.Count = 0;
            topTags.Add(defaultTag);
        }

        return topTags;
    }

    private List<int> GetWordCountTrends(List<JournalEntry> entries, (DateTime Start, DateTime End) range)
    {
        var result = new List<int>();
        var daysInRange = (range.End - range.Start).Days + 1;

        if (daysInRange <= 0) return new List<int> { 0 };

        // Group entries by week for the chart
        var entriesByWeek = new Dictionary<int, List<JournalEntry>>();

        foreach (var entry in entries)
        {
            var weekNum = (int)((entry.Date - range.Start).TotalDays / 7);
            if (!entriesByWeek.ContainsKey(weekNum))
                entriesByWeek[weekNum] = new List<JournalEntry>();
            entriesByWeek[weekNum].Add(entry);
        }

        // Calculate average word count per week
        int maxWeeks = 8; // Show 8 bars max
        for (int week = 0; week < maxWeeks; week++)
        {
            if (entriesByWeek.ContainsKey(week))
            {
                var weekEntries = entriesByWeek[week];
                var avgWords = (int)weekEntries.Average(e =>
                    string.IsNullOrWhiteSpace(e.Content) ? 0 : e.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
                result.Add(avgWords);
            }
            else
            {
                result.Add(0);
            }
        }

        // If we have no data, show some sample pattern
        if (result.All(r => r == 0))
        {
            var rnd = new Random();
            return new List<int> { 120, 180, 150, 240, 210, 300, 260, 340 };
        }

        return result;
    }

    private string GetTagColor(int index)
    {
        return index switch
        {
            0 => "#6C2BD9", // Purple
            1 => "#2F6BFF", // Blue
            2 => "#EC4899", // Pink
            3 => "#16A34A", // Green
            _ => "#6C2BD9"  // Default purple
        };
    }
}