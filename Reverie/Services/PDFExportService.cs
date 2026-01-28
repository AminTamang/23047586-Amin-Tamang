using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Reverie.Models;
using Markdig;
using System.Text;

namespace Reverie.Services;

public class PDFExportService
{
    private readonly MarkdownPipeline _markdownPipeline;

    public PDFExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;

        // Setup Markdig pipeline for markdown processing
        _markdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions() // Includes tables, task lists, etc.
            .UseAutoIdentifiers()    // For heading IDs
            .UseAutoLinks()          // Auto-convert URLs to links
            .Build();
    }

    // Export a single entry (for TodayEntry page)
    public byte[] ExportEntry(JournalEntry entry)
    {
        if (entry == null)
            throw new ArgumentNullException(nameof(entry));

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(50);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Text($"Journal Entry - {entry.Date:MMMM dd, yyyy}")
                    .Bold().FontSize(16).FontColor(QuestPDF.Helpers.Colors.Blue.Darken2);

                page.Content().Column(column =>
                {
                    column.Item().PaddingBottom(10).Text(entry.Title).Bold().FontSize(14);

                    // Mood section
                    if (!string.IsNullOrWhiteSpace(entry.PrimaryMood))
                    {
                        column.Item().PaddingBottom(5).Text($"Mood: {entry.PrimaryMood}");
                    }

                    if (entry.SecondaryMoods?.Any() == true)
                    {
                        column.Item().PaddingBottom(5)
                            .Text($"Secondary Moods: {string.Join(", ", entry.SecondaryMoods)}");
                    }

                    // Tags section
                    if (entry.Tags?.Any() == true)
                    {
                        column.Item().PaddingBottom(10)
                            .Text($"Tags: {string.Join(", ", entry.Tags)}");
                    }

                    column.Item().PaddingBottom(20).LineHorizontal(1);

                    // Content section with Markdig parsing
                    if (!string.IsNullOrWhiteSpace(entry.Content))
                    {
                        // Use Markdig to convert markdown to formatted text
                        column.Item().Text(FormatMarkdownForPDF(entry.Content));
                    }
                    else
                    {
                        column.Item().Text("[No content]").Italic();
                    }

                    column.Item().PaddingTop(20).LineHorizontal(1);
                    column.Item().PaddingTop(5).Text($"Word Count: {GetWordCount(entry.Content)}");
                    column.Item().Text($"Created: {entry.CreatedAt:MMM dd, yyyy h:mm tt}");
                    column.Item().Text($"Updated: {entry.UpdatedAt:MMM dd, yyyy h:mm tt}");
                });

                page.Footer()
                    .AlignCenter()
                    .Text(x => {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
            });
        });

        return document.GeneratePdf();
    }

    // Export multiple entries (for Calendar/Settings page date range)
    public byte[] ExportEntries(List<JournalEntry> entries, string title = "Journal Entries")
    {
        if (entries == null || !entries.Any())
            throw new ArgumentException("No entries to export");

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(50);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Text(title)
                    .Bold().FontSize(16).FontColor(QuestPDF.Helpers.Colors.Blue.Darken2);

                page.Content().Column(column =>
                {
                    column.Item().Text($"Export Date: {DateTime.Now:MMMM dd, yyyy h:mm tt}");
                    column.Item().Text($"Total Entries: {entries.Count}");
                    column.Item().PaddingBottom(20).LineHorizontal(1);

                    foreach (var entry in entries.OrderByDescending(e => e.Date))
                    {
                        column.Item().PaddingBottom(15).Background(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(10).Column(entryColumn =>
                        {
                            entryColumn.Item().Text($"{entry.Date:MMMM dd, yyyy} - {entry.Title}")
                                .Bold().FontSize(12).FontColor(QuestPDF.Helpers.Colors.Blue.Medium);

                            if (!string.IsNullOrWhiteSpace(entry.PrimaryMood))
                            {
                                entryColumn.Item().Text($"Mood: {entry.PrimaryMood}");
                            }

                            if (entry.Tags?.Any() == true)
                            {
                                entryColumn.Item().Text($"Tags: {string.Join(", ", entry.Tags)}");
                            }

                            entryColumn.Item().PaddingTop(5).PaddingBottom(5).LineHorizontal(0.5f);

                            if (!string.IsNullOrWhiteSpace(entry.Content))
                            {
                                // Get preview with markdown stripped
                                var plainText = Markdig.Markdown.ToPlainText(entry.Content, _markdownPipeline);
                                var preview = plainText.Length > 200
                                    ? plainText[..200] + "..."
                                    : plainText;
                                entryColumn.Item().Text(preview);
                            }
                            else
                            {
                                entryColumn.Item().Text("[No content]").Italic();
                            }

                            entryColumn.Item().AlignRight().Text($"Words: {GetWordCount(entry.Content)}");
                        });
                    }
                });

                page.Footer()
                    .AlignCenter()
                    .Text(x => {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
            });
        });

        return document.GeneratePdf();
    }

    // Export analytics (for Dashboard/Settings)
    public byte[] ExportAnalytics(DashboardSummary summary, DateTime startDate, DateTime endDate)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(50);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Text("Journal Analytics Report")
                    .Bold().FontSize(16).FontColor(QuestPDF.Helpers.Colors.Blue.Darken2);

                page.Content().Column(column =>
                {
                    column.Item().Text($"Period: {startDate:MMM dd, yyyy} to {endDate:MMM dd, yyyy}");
                    column.Item().Text($"Generated: {DateTime.Now:MMMM dd, yyyy h:mm tt}");
                    column.Item().PaddingBottom(20).LineHorizontal(1);

                    // Streaks
                    column.Item().PaddingBottom(10).Text("Streak Statistics").Bold().FontSize(14);
                    column.Item().PaddingLeft(10).Text($"Current Streak: {summary.CurrentStreakDays} days");
                    column.Item().PaddingLeft(10).Text($"Longest Streak: {summary.LongestStreakDays} days");
                    column.Item().PaddingLeft(10).Text($"Missed Days: {summary.MissedDays} days");

                    column.Item().PaddingTop(10).PaddingBottom(10).LineHorizontal(0.5f);

                    // Mood Distribution
                    column.Item().PaddingBottom(10).Text("Mood Distribution").Bold().FontSize(14);
                    column.Item().PaddingLeft(10).Text($"Positive: {summary.PositivePercent}%");
                    column.Item().PaddingLeft(10).Text($"Neutral: {summary.NeutralPercent}%");
                    column.Item().PaddingLeft(10).Text($"Negative: {summary.NegativePercent}%");

                    // Mood chart visualization - FIXED LINES 215-219
                    column.Item().PaddingTop(5).PaddingBottom(10).Row(row =>
                    {
                        var total = summary.PositivePercent + summary.NeutralPercent + summary.NegativePercent;
                        if (total > 0)
                        {
                            // Use actual float values, not string interpolation
                            float positiveWidth = summary.PositivePercent;
                            float neutralWidth = summary.NeutralPercent;
                            float negativeWidth = summary.NegativePercent;

                            row.RelativeItem().Height(20).Background(QuestPDF.Helpers.Colors.Green.Medium)
                                .Width(positiveWidth);
                            row.RelativeItem().Height(20).Background(QuestPDF.Helpers.Colors.Yellow.Medium)
                                .Width(neutralWidth);
                            row.RelativeItem().Height(20).Background(QuestPDF.Helpers.Colors.Red.Medium)
                                .Width(negativeWidth);
                        }
                    });

                    column.Item().PaddingTop(10).PaddingBottom(10).LineHorizontal(0.5f);

                    // Top Tags
                    column.Item().PaddingBottom(10).Text("Most Used Tags").Bold().FontSize(14);
                    foreach (var tag in summary.TopTags)
                    {
                        column.Item().PaddingLeft(10).Text($"{tag.Name}: {tag.Count} entries");
                    }

                    column.Item().PaddingTop(10).PaddingBottom(10).LineHorizontal(0.5f);

                    // Word Count Trend
                    column.Item().PaddingBottom(10).Text("Writing Activity").Bold().FontSize(14);
                    if (summary.WordCounts.Any())
                    {
                        column.Item().PaddingLeft(10).Text($"Average words per entry: {summary.WordCounts.Average():F0}");
                        column.Item().PaddingLeft(10).Text($"Total segments tracked: {summary.WordCounts.Count}");

                        // Simple bar chart for word count trends - FIXED LINE 249
                        column.Item().PaddingTop(5).PaddingBottom(10).Row(row =>
                        {
                            var maxCount = summary.WordCounts.Max();
                            foreach (var count in summary.WordCounts)
                            {
                                float heightPercent = maxCount > 0 ? (count * 100.0f / maxCount) : 0;
                                row.RelativeItem().Height(20).Background(QuestPDF.Helpers.Colors.Blue.Lighten2)
                                    .Height(heightPercent);
                            }
                        });
                    }
                    else
                    {
                        column.Item().PaddingLeft(10).Text("No word count data available").Italic();
                    }
                });

                page.Footer()
                    .AlignCenter()
                    .Text("Confidential - Your Personal Journal");
            });
        });

        return document.GeneratePdf();
    }

    // Helper method to format markdown for PDF
    private string FormatMarkdownForPDF(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        // Convert markdown to plain text for PDF
        var plainText = Markdig.Markdown.ToPlainText(markdown, _markdownPipeline);

        // Clean up extra whitespace
        plainText = plainText.Trim();

        // Replace multiple newlines with single newlines
        plainText = System.Text.RegularExpressions.Regex.Replace(plainText, @"\n{3,}", "\n\n");

        return plainText;
    }

    // Get word count using Markdig for accurate counting
    private int GetWordCount(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 0;

        // Convert markdown to plain text first
        var plainText = Markdig.Markdown.ToPlainText(content, _markdownPipeline);

        // Count words from plain text
        return plainText.Split(new[] { ' ', '\n', '\r', '\t' },
            StringSplitOptions.RemoveEmptyEntries).Length;
    }

    // New method: Export a single entry with full HTML formatting
    public byte[] ExportFormattedEntry(JournalEntry entry)
    {
        if (entry == null)
            throw new ArgumentNullException(nameof(entry));

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11));

                // Header with decorative line
                page.Header().Column(column =>
                {
                    column.Item().Text($"Journal Entry - {entry.Date:MMMM dd, yyyy}")
                        .Bold().FontSize(18).FontColor(QuestPDF.Helpers.Colors.Blue.Darken2);

                    column.Item().PaddingTop(5).LineHorizontal(1).LineColor(QuestPDF.Helpers.Colors.Blue.Lighten1);
                });

                page.Content().Column(column =>
                {
                    // Title section
                    column.Item().PaddingBottom(15).Text(entry.Title)
                        .Bold().FontSize(16).FontColor(QuestPDF.Helpers.Colors.Grey.Darken3);

                    // Metadata in a subtle box
                    column.Item().PaddingBottom(20).Background(QuestPDF.Helpers.Colors.Grey.Lighten4).Padding(10).Column(metaColumn =>
                    {
                        if (!string.IsNullOrWhiteSpace(entry.PrimaryMood))
                        {
                            metaColumn.Item().PaddingBottom(3).Row(row =>
                            {
                                row.ConstantItem(80).Text("Primary Mood:").SemiBold();
                                row.RelativeItem().Text(entry.PrimaryMood);
                            });
                        }

                        if (entry.SecondaryMoods?.Any() == true)
                        {
                            metaColumn.Item().PaddingBottom(3).Row(row =>
                            {
                                row.ConstantItem(80).Text("Secondary Moods:").SemiBold();
                                row.RelativeItem().Text(string.Join(", ", entry.SecondaryMoods));
                            });
                        }

                        if (entry.Tags?.Any() == true)
                        {
                            metaColumn.Item().Row(row =>
                            {
                                row.ConstantItem(80).Text("Tags:").SemiBold();
                                row.RelativeItem().Text(string.Join(", ", entry.Tags));
                            });
                        }
                    });

                    // Content section with better formatting
                    column.Item().PaddingBottom(20).Text("Entry Content").Bold().FontSize(14);
                    column.Item().PaddingBottom(5).LineHorizontal(0.5f).LineColor(QuestPDF.Helpers.Colors.Grey.Lighten2);

                    if (!string.IsNullOrWhiteSpace(entry.Content))
                    {
                        // Parse markdown into structured content
                        var lines = entry.Content.Split('\n');
                        foreach (var line in lines)
                        {
                            if (line.StartsWith("# "))
                            {
                                column.Item().PaddingTop(15).PaddingBottom(5).Text(line[2..])
                                    .Bold().FontSize(16).FontColor(QuestPDF.Helpers.Colors.Blue.Darken2);
                            }
                            else if (line.StartsWith("## "))
                            {
                                column.Item().PaddingTop(10).PaddingBottom(3).Text(line[3..])
                                    .Bold().FontSize(14).FontColor(QuestPDF.Helpers.Colors.Grey.Darken2);
                            }
                            else if (line.StartsWith("### "))
                            {
                                column.Item().PaddingTop(8).PaddingBottom(2).Text(line[4..])
                                    .Bold().FontSize(12);
                            }
                            else if (line.StartsWith("* ") || line.StartsWith("- "))
                            {
                                column.Item().PaddingLeft(15).Text($"• {line[2..]}");
                            }
                            else if (line.StartsWith("1. "))
                            {
                                column.Item().PaddingLeft(15).Text($"1. {line[3..]}");
                            }
                            else if (!string.IsNullOrWhiteSpace(line))
                            {
                                column.Item().PaddingTop(2).Text(line);
                            }
                            else
                            {
                                // Empty line for paragraph spacing
                                column.Item().PaddingTop(5);
                            }
                        }
                    }
                    else
                    {
                        column.Item().Text("[No content]").Italic().FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
                    }

                    // Footer section
                    column.Item().PaddingTop(30).Column(footerColumn =>
                    {
                        footerColumn.Item().PaddingTop(10).LineHorizontal(0.5f).LineColor(QuestPDF.Helpers.Colors.Grey.Lighten2);

                        footerColumn.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text($"Word Count: {GetWordCount(entry.Content)}").SemiBold();
                                col.Item().Text($"Created: {entry.CreatedAt:MMM dd, yyyy h:mm tt}");
                                col.Item().Text($"Updated: {entry.UpdatedAt:MMM dd, yyyy h:mm tt}");
                            });

                            row.ConstantItem(150).AlignRight().Column(col =>
                            {
                                col.Item().Text("Reverie Journal").FontSize(9).FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
                                col.Item().Text("Confidential").FontSize(8).FontColor(QuestPDF.Helpers.Colors.Grey.Lighten1);
                            });
                        });
                    });
                });

                // Page footer - FIXED LINE 440 (CS0023 error)
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                    x.Span($" • {DateTime.Now:MMM dd, yyyy}");
                });
            });
        });

        return document.GeneratePdf();
    }
}