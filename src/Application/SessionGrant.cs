namespace Snail.MCP.Documents.Application;

/// <summary>Which tools this session may use, read from the address it opened.</summary>
/// <remarks>
/// The platform gives a role a list of tools and appends it to the address as <c>&amp;tool=…</c>, once per
/// tool. It is fixed when the session opens, the model cannot reach it, and it is checked where calls
/// arrive rather than inside each tool. A session that names no tool may use them all, which is what a
/// server started as a program for one caller has always been.
/// </remarks>
public sealed class SessionGrant(ISessionAddress address)
{
    /// <summary>What the address calls a tool it allows; repeat it for each one.</summary>
    public const string Parameter = "tool";

    /// <summary>The tools this session named, empty when it named none and may use everything.</summary>
    public IReadOnlyList<string> All =>
    [
        .. address.Values(Parameter)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
    ];

    /// <summary>Whether this session may call that tool.</summary>
    public bool Allows(string name)
    {
        var granted = All;

        return granted.Count == 0 || granted.Contains(name, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>The refusal, in the words a caller needs to stop asking.</summary>
    public string Refusal(string name) =>
        $"'{name}' was not granted to this session, which may use {string.Join(", ", All)}";
}
