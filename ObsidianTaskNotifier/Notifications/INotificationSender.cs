using ObsidianTaskNotifier.Models;

namespace ObsidianTaskNotifier.Notifications;

/// <summary>
///     Sends notification messages through a specific channel.
/// </summary>
public interface INotificationSender
{
    /// <summary>
    ///     Sends the message. Implementations are expected to log and swallow failures.
    /// </summary>
    Task SendAsync(NotificationMessage message, CancellationToken ct);
}
