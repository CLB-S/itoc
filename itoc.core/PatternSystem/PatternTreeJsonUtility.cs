using Godot;
using FileAccess = Godot.FileAccess;

namespace ITOC.Core.PatternSystem;

/// <summary>
///     Utility class for saving pattern trees to files and loading them from files.
/// </summary>
public static class PatternTreeJsonUtility
{
    /// <summary>
    ///     Save a pattern tree to a file.
    /// </summary>
    /// <param name="node">The pattern tree to save</param>
    /// <param name="filePath">Path to save the file to</param>
    public static void SaveToFile(PatternTreeNode node, string filePath)
    {
        var json = PatternTreeJsonConverter.Serialize(node);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    ///     Load a pattern tree from a file.
    /// </summary>
    /// <param name="filePath">Path to load the file from</param>
    /// <returns>The loaded pattern tree</returns>
    public static PatternTreeNode LoadFromFile(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return PatternTreeJsonConverter.Deserialize(json);
    }

    /// <summary>
    ///     Save a pattern tree to a file asynchronously.
    /// </summary>
    /// <param name="node">The pattern tree to save</param>
    /// <param name="filePath">Path to save the file to</param>
    public static async Task SaveToFileAsync(PatternTreeNode node, string filePath)
    {
        var json = PatternTreeJsonConverter.Serialize(node);
        await File.WriteAllTextAsync(filePath, json);
    }

    /// <summary>
    ///     Load a pattern tree from a file asynchronously.
    /// </summary>
    /// <param name="filePath">Path to load the file from</param>
    /// <returns>The loaded pattern tree</returns>
    public static async Task<PatternTreeNode> LoadFromFileAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        return PatternTreeJsonConverter.Deserialize(json);
    }

    /// <summary>
    ///     Save a pattern tree to a Godot resource file.
    /// </summary>
    /// <param name="node">The pattern tree to save</param>
    /// <param name="filePath">Path to save the file to</param>
    public static void SaveToGodotResource(PatternTreeNode node, string filePath)
    {
        var json = PatternTreeJsonConverter.Serialize(node);
        using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Write);
        if (file != null)
            file.StoreString(json);
        else
            GD.PrintErr($"Failed to save pattern tree to {filePath}. Error: {FileAccess.GetOpenError()}");
    }

    /// <summary>
    ///     Load a pattern tree from a Godot resource file.
    /// </summary>
    /// <param name="filePath">Path to load the file from</param>
    /// <returns>The loaded pattern tree or null if loading failed</returns>
    public static PatternTreeNode LoadFromGodotResource(string filePath)
    {
        using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
        if (file != null)
        {
            var json = file.GetAsText();
            return PatternTreeJsonConverter.Deserialize(json);
        }

        GD.PrintErr($"Failed to load pattern tree from {filePath}. Error: {FileAccess.GetOpenError()}");
        return null;
    }

    #region PatternTree Specific Methods

    /// <summary>
    ///     Save a PatternTree to a file with its ID as the filename.
    /// </summary>
    /// <param name="patternTree">The pattern tree to save</param>
    /// <param name="directory">Directory to save the file to</param>
    /// <returns>The full path to the saved file</returns>
    public static string SavePatternTree(PatternTree patternTree, string directory)
    {
        var fileName = $"{patternTree.Id}.pattern";
        var filePath = Path.Combine(directory, fileName);
        SaveToFile(patternTree, filePath);
        return filePath;
    }

    /// <summary>
    ///     Load a PatternTree from a file.
    /// </summary>
    /// <param name="filePath">Path to load the file from</param>
    /// <returns>The loaded PatternTree</returns>
    public static PatternTree LoadPatternTree(string filePath)
    {
        var node = LoadFromFile(filePath);
        if (node is PatternTree patternTree) return patternTree;
        throw new InvalidOperationException($"File {filePath} does not contain a valid PatternTree");
    }

    /// <summary>
    ///     Save a pattern tree to a Godot resource file with its ID as the filename.
    /// </summary>
    /// <param name="patternTree">The pattern tree to save</param>
    /// <param name="directory">Directory to save the file to</param>
    /// <returns>The full path to the saved file</returns>
    public static string SavePatternTreeToGodotResource(PatternTree patternTree, string directory)
    {
        var fileName = $"{patternTree.Id}.tres";
        var filePath = Path.Combine(directory, fileName);
        SaveToGodotResource(patternTree, filePath);
        return filePath;
    }

    /// <summary>
    ///     Load a PatternTree from a Godot resource file.
    /// </summary>
    /// <param name="filePath">Path to load the file from</param>
    /// <returns>The loaded PatternTree or null if loading failed</returns>
    public static PatternTree LoadPatternTreeFromGodotResource(string filePath)
    {
        var node = LoadFromGodotResource(filePath);
        if (node is PatternTree patternTree) return patternTree;
        throw new InvalidOperationException($"File {filePath} does not contain a valid PatternTree");
    }

    /// <summary>
    ///     Load all pattern trees from a directory.
    /// </summary>
    /// <param name="directory">Directory to load patterns from</param>
    /// <param name="searchPattern">Search pattern for files (default: *.pattern)</param>
    /// <returns>Array of loaded PatternTree objects</returns>
    public static PatternTree[] LoadAllPatternTrees(string directory, string searchPattern = "*.pattern")
    {
        if (!Directory.Exists(directory))
            return Array.Empty<PatternTree>();

        var files = Directory.GetFiles(directory, searchPattern);
        var patterns = new PatternTree[files.Length];

        for (var i = 0; i < files.Length; i++)
            try
            {
                var node = LoadFromFile(files[i]);
                if (node is PatternTree patternTree)
                    patterns[i] = patternTree;
                else
                    GD.PrintErr($"File {files[i]} does not contain a valid PatternTree");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Failed to load pattern from {files[i]}: {ex.Message}");
            }

        return patterns;
    }

    #endregion
}