namespace Snail.MCP.Documents.Domain;

/// <summary>A path this session may read, resolved, or the reason it may not.</summary>
public sealed record Reached(string? Path, string? Error, string? Hint)
{
    /// <summary>Whether the path may be read.</summary>
    public bool IsAllowed => Path is not null;

    /// <summary>The path as it will be read.</summary>
    public static Reached Yes(string path) => new(path, null, null);

    /// <summary>The refusal, with what to do instead.</summary>
    public static Reached No(string error, string? hint = null) => new(null, error, hint);
}
