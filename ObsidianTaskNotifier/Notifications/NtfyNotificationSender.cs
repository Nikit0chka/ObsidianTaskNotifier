using System.Text;
using Microsoft.Extensions.Logging;
using ObsidianTaskNotifier.Configuration;
using ObsidianTaskNotifier.Models;

namespace ObsidianTaskNotifier.Notifications;

/// <inheritdoc />
/// <summary>
///     Publishes notifications to a ntfy topic using the HTTP publish API.
/// </summary>
public sealed class NtfyNotificationSender(
    HttpClient httpClient,
    AppConfig config,
    ILogger<NtfyNotificationSender> logger)
    : INotificationSender
{
    public async Task SendAsync(NotificationMessage message, CancellationToken ct)
    {
        var url = $"{config.NtfyBaseUrl.TrimEnd('/')}/{config.NtfyTopic}";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);

        request.Content = new StringContent(message.Body, Encoding.UTF8, "text/plain");

        request.Headers.Add("Title", message.Title);
        request.Headers.Add("Priority", NotificationMessage.Priority);
        request.Headers.Add("Markdown", "yes");

        try
        {
            using var response = await httpClient.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
                logger.LogInformation("Notification sent to {Url} (status {Status})", url, (int)response.StatusCode);
            else
                logger.LogError("ntfy returned non-success status {Status} for {Url}", (int)response.StatusCode, url);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send notification to {Url}", url);
        }
    }
}
