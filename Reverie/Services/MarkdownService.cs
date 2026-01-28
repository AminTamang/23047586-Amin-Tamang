using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Markdig;

namespace Reverie.Services;

public class MarkdownService
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownService()
    {
        // Configure the pipeline with extensions
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions() // Includes tables, task lists, etc.
            .UseEmojiAndSmiley()    // Optional: for emoji support
            .UseAutoLinks()         // Auto-convert URLs to links
            .Build();
    }

    // Convert markdown to HTML
    public string ToHtml(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        return Markdig.Markdown.ToHtml(markdown, _pipeline);
    }

    // Convert HTML to markdown (basic implementation)
    public string ToMarkdown(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        // Remove simple HTML tags (Markdig doesn't have HTML-to-markdown)
        // For now, use a simple converter
        return SimpleHtmlToMarkdown(html);
    }

    private string SimpleHtmlToMarkdown(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        string markdown = html;

        // Headings
        markdown = markdown.Replace("<h1>", "# ").Replace("</h1>", "\n\n");
        markdown = markdown.Replace("<h2>", "## ").Replace("</h2>", "\n\n");
        markdown = markdown.Replace("<h3>", "### ").Replace("</h3>", "\n\n");

        // Bold/Strong
        markdown = markdown.Replace("<strong>", "**").Replace("</strong>", "**");
        markdown = markdown.Replace("<b>", "**").Replace("</b>", "**");

        // Italic/Em
        markdown = markdown.Replace("<em>", "*").Replace("</em>", "*");
        markdown = markdown.Replace("<i>", "*").Replace("</i>", "*");

        // Underline (markdown doesn't support underline, use emphasis)
        markdown = markdown.Replace("<u>", "*").Replace("</u>", "*");

        // Links
        markdown = System.Text.RegularExpressions.Regex.Replace(
            markdown,
            @"<a\s+[^>]*href=""([^""]*)""[^>]*>([^<]*)</a>",
            "[$2]($1)"
        );

        // Lists
        markdown = markdown.Replace("<li>", "- ").Replace("</li>", "\n");
        markdown = markdown.Replace("<ul>", "").Replace("</ul>", "\n");
        markdown = markdown.Replace("<ol>", "").Replace("</ol>", "\n");

        // Paragraphs and line breaks
        markdown = markdown.Replace("<p>", "").Replace("</p>", "\n\n");
        markdown = markdown.Replace("<br>", "\n").Replace("<br/>", "\n").Replace("<br />", "\n");

        // Remove any remaining HTML tags
        markdown = System.Text.RegularExpressions.Regex.Replace(
            markdown,
            @"<[^>]*>",
            string.Empty
        );

        // Clean up extra whitespace
        markdown = markdown.Trim();
        markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"\n{3,}", "\n\n");

        return markdown;
    }

    // Get plain text (for word count)
    public string GetPlainText(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        // Convert to HTML first
        var html = ToHtml(markdown);

        // Remove HTML tags
        var plain = System.Text.RegularExpressions.Regex.Replace(
            html,
            @"<[^>]*>",
            string.Empty
        );

        // Decode HTML entities
        plain = System.Net.WebUtility.HtmlDecode(plain);

        return plain;
    }

    // Word count from markdown
    public int GetWordCount(string markdown)
    {
        var plainText = GetPlainText(markdown);
        if (string.IsNullOrWhiteSpace(plainText))
            return 0;

        return plainText.Split(new[] { ' ', '\n', '\r', '\t' },
            StringSplitOptions.RemoveEmptyEntries).Length;
    }

    // Preview (first X characters)
    public string GetPreview(string markdown, int maxLength = 150)
    {
        var plainText = GetPlainText(markdown);
        if (string.IsNullOrWhiteSpace(plainText))
            return string.Empty;

        if (plainText.Length <= maxLength)
            return plainText;

        return plainText.Substring(0, maxLength) + "...";
    }
}