namespace ObsidianTaskNotifier.Models;

/// <summary>
///     A single task parsed from a Markdown file.
/// </summary>
public sealed record TaskItem
{
    public required string Description { get; init; }
    public required DateTime Start { get; init; }
    public required DateTime End { get; init; }
    public required string File { get; init; }

    /// <summary>
    ///     Stable identifier used to track whether a notification has already been sent.
    /// </summary>
    internal string Key => $"{File}:{Description}:{Start:yyyy-MM-dd HH:mm}";
}
