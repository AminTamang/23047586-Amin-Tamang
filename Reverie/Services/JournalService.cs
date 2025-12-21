using System;
using System.Collections.Generic;
using System.Linq;

using Reverie.Models;

namespace Reverie.Services;

public class JournalService
{
    private readonly Dictionary<DateTime, JournalEntry> _entries = new();

    public JournalEntry? GetByDate(DateTime date)
    {
        _entries.TryGetValue(date.Date, out var entry);
        return entry;
    }

    public IEnumerable<JournalEntry> GetAll()
        => _entries.Values.OrderByDescending(e => e.Date);

    public void Save(JournalEntry entry)
    {
        if (!_entries.ContainsKey(entry.Date))
            entry.CreatedAt = DateTime.Now;

        entry.UpdatedAt = DateTime.Now;
        _entries[entry.Date] = entry;
    }

    public void Delete(DateTime date)
    {
        _entries.Remove(date.Date);
    }

    public int GetCurrentStreak()
    {
        int streak = 0;
        var date = DateTime.Today;
        while (_entries.ContainsKey(date))
        {
            streak++;
            date = date.AddDays(-1);
        }
        return streak;
    }
}
