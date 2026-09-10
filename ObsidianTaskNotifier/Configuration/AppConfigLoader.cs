using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ObsidianTaskNotifier.Configuration;

/// <summary>
///     Reads and validates <see cref="AppConfig" /> from the provided <see cref="IConfiguration" />.
/// </summary>
public sealed class AppConfigLoader(IConfiguration configuration, ILogger<AppConfigLoader> logger)
{
    /// <summary>
    ///     Builds the configuration, throwing when required values are missing.
    /// </summary>
    internal AppConfig Load()
    {
        var ntfyTopic = configuration["NTFY_TOPIC"];
        if (string.IsNullOrWhiteSpace(ntfyTopic))
            throw new InvalidOperationException("Environment variable NTFY_TOPIC is required.");

        var config = new AppConfig
        {
            VaultPath = configuration["VAULT_PATH"] ?? ".",
            NtfyTopic = ntfyTopic,
            NtfyBaseUrl = configuration["NTFY_BASE_URL"] ?? "https://ntfy.sh",
            NtfyTitle = configuration["NTFY_TITLE"] ?? "Task Reminder",
            WarningWindow = TimeSpan.FromMinutes(GetInt("WARNING_WINDOW_MINUTES", 5)),
            StateFilePath = configuration["STATE_FILE_PATH"]
                            ?? Path.Combine(AppContext.BaseDirectory, "state.json"),
            StateRetentionDays = GetInt("STATE_RETENTION_DAYS", 7),
            ExecutionTimeout = TimeSpan.FromSeconds(GetInt("EXECUTION_TIMEOUT_SECONDS", 120))
        };

        logger.LogInformation(
            "Configuration loaded: VaultPath={VaultPath}, NtfyBaseUrl={NtfyBaseUrl}, WarningWindow={WarningWindow}, StateFilePath={StateFilePath}, RetentionDays={RetentionDays}, Timeout={Timeout}",
            config.VaultPath, config.NtfyBaseUrl, config.WarningWindow,
            config.StateFilePath, config.StateRetentionDays, config.ExecutionTimeout);

        return config;
    }

    private int GetInt(string key, int defaultValue)
    {
        var raw = configuration[key];
        if (string.IsNullOrWhiteSpace(raw))
            return defaultValue;

        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            return value;

        logger.LogWarning("Invalid integer for {Key}='{Raw}', falling back to {Default}", key, raw, defaultValue);
        return defaultValue;
    }
}
