using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ObsidianTaskNotifier.Configuration;
using ObsidianTaskNotifier.Notifications;
using ObsidianTaskNotifier.Parsing;
using ObsidianTaskNotifier.Services;
using ObsidianTaskNotifier.State;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(static options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
});

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<AppConfigLoader>();
builder.Services.AddSingleton<AppConfig>(static sp => sp.GetRequiredService<AppConfigLoader>().Load());
builder.Services.AddSingleton<ITaskParser, MarkdownTaskParser>();
builder.Services.AddSingleton<IStateRepository, JsonStateRepository>();
builder.Services.AddHttpClient<INotificationSender, NtfyNotificationSender>();
builder.Services.AddSingleton<ReminderService>();

var host = builder.Build();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

try
{
    var config = host.Services.GetRequiredService<AppConfig>();

    using var cts = new CancellationTokenSource(config.ExecutionTimeout);

    var service = host.Services.GetRequiredService<ReminderService>();
    await service.RunAsync(cts.Token);

    return 0;
}
catch (OperationCanceledException)
{
    logger.LogError("Reminder run was cancelled or timed out");
    return 1;
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Unhandled exception during reminder run");
    return 1;
}
