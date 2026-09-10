namespace ObsidianTaskNotifier.Configuration;

/// <summary>
///     Immutable application configuration resolved from environment and appsettings.
/// </summary>
public sealed record AppConfig
{
    public required string VaultPath { get; init; }
    public required string NtfyTopic { get; init; }
    public required string NtfyBaseUrl { get; init; }
    public required string NtfyTitle { get; init; }
    public required TimeSpan WarningWindow { get; init; }
    public required string StateFilePath { get; init; }
    public required int StateRetentionDays { get; init; }
    public required TimeSpan ExecutionTimeout { get; init; }
}
