using Microsoft.Extensions.Logging;
using ObsidianTaskNotifier.Configuration;
using ObsidianTaskNotifier.Models;
using ObsidianTaskNotifier.Notifications;
using ObsidianTaskNotifier.Parsing;
using ObsidianTaskNotifier.State;

namespace ObsidianTaskNotifier.Services;

/// <summary>
///     Orchestrates a single reminder run: parse tasks, filter the warning window,
///     send notifications for unseen tasks and persist the updated state.
/// </summary>
public sealed class ReminderService(
    ITaskParser parser,
    IStateRepository stateRepository,
    INotificationSender notificationSender,
    AppConfig config,
    TimeProvider timeProvider,
    ILogger<ReminderService> logger)
{
    /// <summary>
    ///     Runs the reminder pipeline once.
    /// </summary>
    internal async Task RunAsync(CancellationToken ct)
    {
        var now = timeProvider.GetLocalNow().DateTime;
        logger.LogInformation("Reminder run started at {Now}", now);

        var state = new Dictionary<string, DateTime>(
            await stateRepository.LoadAsync(ct));

        PruneOldEntries(state, now);

        var tasks = parser.ParseFromDirectory(config.VaultPath);
        logger.LogInformation("Found {Count} tasks in vault", tasks.Count);

        var windowEnd = now + config.WarningWindow;
        var upcoming = tasks
            .Where(t => t.Start >= now && t.Start <= windowEnd)
            .OrderBy(static t => t.Start)
            .ToList();

        logger.LogInformation("Tasks within the next {Window}: {Count}", config.WarningWindow, upcoming.Count);

        var sentCount = 0;
        foreach (var task in upcoming)
        {
            if (state.ContainsKey(task.Key))
            {
                logger.LogDebug("Skipping already-notified task {Key}", task.Key);
                continue;
            }

            var message = new NotificationMessage
            {
                Title = config.NtfyTitle,
                Body = BuildBody(task, task.Start - now)
            };

            await notificationSender.SendAsync(message, ct);
            state[task.Key] = now;
            sentCount++;
        }

        logger.LogInformation("Sent {Count} notifications", sentCount);

        await stateRepository.SaveAsync(state, ct);
        logger.LogInformation("Reminder run finished");
    }

    private void PruneOldEntries(Dictionary<string, DateTime> state, DateTime now)
    {
        var cutoff = now.AddDays(-config.StateRetentionDays);
        var stale = state
            .Where(kv => kv.Value < cutoff)
            .Select(static kv => kv.Key)
            .ToList();

        foreach (var key in stale)
        {
            state.Remove(key);
        }

        if (stale.Count > 0)
            logger.LogInformation("Pruned {Count} state entries older than {Cutoff:yyyy-MM-dd}", stale.Count, cutoff);
    }

    private static string BuildBody(TaskItem task, TimeSpan timeLeft)
    {
        var minutes = (int)Math.Round(timeLeft.TotalMinutes);
        return $"⏰ Через {minutes} мин: <b>{task.Description}</b>\n" +
               $"🕒 {task.Start:HH:mm} – {task.End:HH:mm}";
    }
}
