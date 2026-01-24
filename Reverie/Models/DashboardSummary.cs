using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reverie.Models;

public class DashboardSummary
{
    public int CurrentStreakDays { get; set; }
    public int LongestStreakDays { get; set; }
    public int MissedDays { get; set; }

    // Mood %
    public int PositivePercent { get; set; }
    public int NeutralPercent { get; set; }
    public int NegativePercent { get; set; }

    // Tags
    public List<TagUsage> TopTags { get; set; } = new();

    // Word count trend (simple list)
    public List<int> WordCounts { get; set; } = new();
}

public class TagUsage
{
    public string Name { get; set; } = "";
    public int Count { get; set; }
    public string ColorHex { get; set; } = "#6C2BD9";
}
