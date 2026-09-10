using ObsidianTaskNotifier.Models;

namespace ObsidianTaskNotifier.Parsing;

/// <summary>
///     Extracts tasks from a source directory.
/// </summary>
public interface ITaskParser
{
    /// <summary>
    ///     Parses all supported files under <paramref name="rootPath" />.
    /// </summary>
    IReadOnlyList<TaskItem> ParseFromDirectory(string rootPath);
}
