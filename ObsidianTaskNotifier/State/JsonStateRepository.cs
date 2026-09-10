using System.Text.Json;
using Microsoft.Extensions.Logging;
using ObsidianTaskNotifier.Configuration;

namespace ObsidianTaskNotifier.State;

/// <inheritdoc />
/// <summary>
///     JSON-file backed <see cref="T:IStateRepository">IStateRepository</see> with atomic writes.
/// </summary>
public sealed class JsonStateRepository(AppConfig config, ILogger<JsonStateRepository> logger) : IStateRepository
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _stateFilePath = config.StateFilePath;

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, DateTime>> LoadAsync(CancellationToken ct)
    {
        if (!File.Exists(_stateFilePath))
        {
            logger.LogInformation("State file not found at {Path}, starting fresh", _stateFilePath);
            return new Dictionary<string, DateTime>();
        }

        try
        {
            await using var stream = File.OpenRead(_stateFilePath);
            var state = await JsonSerializer.DeserializeAsync<Dictionary<string, DateTime>>(
                stream, _jsonOptions, ct);

            var result = state ?? new Dictionary<string, DateTime>();
            logger.LogInformation("Loaded {Count} state entries from {Path}", result.Count, _stateFilePath);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load state from {Path}, starting fresh", _stateFilePath);
            return new Dictionary<string, DateTime>();
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(IReadOnlyDictionary<string, DateTime> state, CancellationToken ct)
    {
        var directory = Path.GetDirectoryName(_stateFilePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var tmpPath = _stateFilePath + ".tmp";

        try
        {
            await using (var stream = File.Create(tmpPath))
            {
                await JsonSerializer.SerializeAsync(stream, state, _jsonOptions, ct);
            }

            File.Move(tmpPath, _stateFilePath, true);
            logger.LogInformation("Saved {Count} state entries to {Path}", state.Count, _stateFilePath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save state to {Path}", _stateFilePath);
        }
    }
}
