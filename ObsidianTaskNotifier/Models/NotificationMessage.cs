namespace ObsidianTaskNotifier.Models;

/// <summary>
///     Channel-agnostic notification payload.
/// </summary>
public sealed record NotificationMessage
{
    public required string Title { get; init; }
    public required string Body { get; init; }
    internal static string Priority => "high";
}
