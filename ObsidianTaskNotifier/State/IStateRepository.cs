namespace ObsidianTaskNotifier.State;

/// <summary>
///     Persists notification state between application runs.
/// </summary>
public interface IStateRepository
{
    /// <summary>
    ///     Loads the state as a map of notification key to the moment it was sent.
    ///     Returns an empty map when the state file is missing or unreadable.
    /// </summary>
    Task<IReadOnlyDictionary<string, DateTime>> LoadAsync(CancellationToken ct);

    /// <summary>
    ///     Atomically persists the provided state.
    /// </summary>
    Task SaveAsync(IReadOnlyDictionary<string, DateTime> state, CancellationToken ct);
}
