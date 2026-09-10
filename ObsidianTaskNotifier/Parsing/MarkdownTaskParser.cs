using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using ObsidianTaskNotifier.Models;

namespace ObsidianTaskNotifier.Parsing;

/// <inheritdoc />
/// <summary>
///     Parses Markdown checklists in the format <c>- [ ] HH:mm - HH:mm description ⏳ yyyy-MM-dd</c>.
/// </summary>
public sealed partial class MarkdownTaskParser(ILogger<MarkdownTaskParser> logger) : ITaskParser
{
    [GeneratedRegex(
        @"-\s*\[ \]\s*(?<start>\d{2}:\d{2})\s*-\s*(?<end>\d{2}:\d{2})\s+(?<desc>.*?)\s*⏳\s*(?<date>\d{4}-\d{2}-\d{2})",
        RegexOptions.Compiled)]
    private static partial Regex TaskRegex();

    /// <inheritdoc />
    public IReadOnlyList<TaskItem> ParseFromDirectory(string rootPath)
    {
        var tasks = new List<TaskItem>();

        if (!Directory.Exists(rootPath))
        {
            logger.LogWarning("Vault path does not exist: {Path}", rootPath);
            return tasks;
        }

        var mdFiles = Directory
            .EnumerateFiles(rootPath, "*.md", SearchOption.AllDirectories)
            .Where(static f => !f.Contains(".git", StringComparison.OrdinalIgnoreCase));

        foreach (var file in mdFiles)
        {
            string content;
            try
            {
                content = File.ReadAllText(file);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to read file {File}", file);
                continue;
            }

            foreach (Match match in TaskRegex().Matches(content))
            {
                var task = TryParse(match, file);
                if (task is not null)
                    tasks.Add(task);
            }
        }

        return tasks;
    }

    private TaskItem? TryParse(Match match, string file)
    {
        var dateStr = match.Groups["date"].Value;
        var startStr = match.Groups["start"].Value;
        var endStr = match.Groups["end"].Value;
        var description = match.Groups["desc"].Value.Trim();

        if (!DateTime.TryParseExact(
                $"{dateStr} {startStr}", "yyyy-MM-dd HH:mm",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var start))
        {
            logger.LogDebug("Failed to parse start time in {File}: '{Value}'", file, startStr);
            return null;
        }

        if (DateTime.TryParseExact(
                $"{dateStr} {endStr}", "yyyy-MM-dd HH:mm",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
        {
            return new TaskItem
            {
                Description = description,
                Start = start,
                End = end,
                File = file
            };
        }

        logger.LogDebug("Failed to parse end time in {File}: '{Value}'", file, endStr);
        return null;
    }
}
