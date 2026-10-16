using Snail.MCP.Documents.Configuration;

namespace Snail.MCP.Documents.Application;

/// <summary>The folders this session may read, and whether a path is inside one of them.</summary>
/// <remarks>
/// A reader of files is one call away from every file its process can open, so this is the whole of its
/// security and every path goes through it. The roots are read from the address the session was opened at —
/// <c>…/mcp?root=/workspaces/task</c> — and not from anything a call carries, because a call can say whatever
/// it likes; a session that names none gets the configured roots, which is how a server started as a program
/// for one caller is confined. Closed by default: a server with no roots at all reads nothing rather than the disk.
/// </remarks>
public sealed class SessionRoots(ServerConfig config, ISessionAddress address, IDisk disk)
{
    /// <summary>What the address calls the root it hands over; repeat it for more than one.</summary>
    public const string Parameter = "root";

    /// <summary>The roots of this session: the ones its address names, or the configured ones when it names none.</summary>
    /// <remarks>
    /// Either, never both. A shared server is given the whole volume of working copies as its configured root
    /// so that it can serve any of them — adding that to what the session named would hand every session every
    /// other task's files, which is exactly the isolation this exists to keep.
    /// </remarks>
    public IReadOnlyList<string> All => Paths(address.Values(Parameter)) is { Count: > 0 } named ? named : Paths(config.Roots);

    /// <summary>Of the roots a session names, the ones this machine does not have.</summary>
    /// <remarks>
    /// Asked before a session is let in, so that a worker pointed at the server of another machine hears about
    /// it at once: a root that does not exist here means the copy is on a different host, and every answer this
    /// session could give would be about the wrong files.
    /// </remarks>
    public static IReadOnlyList<string> Absent(IEnumerable<string> named, IDisk disk) =>
    [
        .. named
            .Where(root => !string.IsNullOrWhiteSpace(root))
            .Select(root => Path.GetFullPath(root.Trim()))
            .Where(root => !disk.IsFolder(root))
            .Distinct(StringComparer.Ordinal)
    ];

    /// <summary>Of the roots a session names, the ones outside every configured root.</summary>
    /// <remarks>
    /// The configured root of a shared server is the volume of the machine's working copies, and a session names one copy
    /// in it. Without this check a session could name any folder the process can see — another task's copy, the server's
    /// own files — and the only test it had to pass was that the folder exists. A server configured with no root serves
    /// whatever its sessions name, which is how a program started for one caller has always been.
    /// </remarks>
    public static IReadOnlyList<string> Outside(IEnumerable<string> named, IEnumerable<string> configured, IDisk disk)
    {
        var volumes = Paths(configured).Select(disk.Real).ToList();
        if (volumes.Count == 0)
            return [];

        var roots = Paths(named);

        return [.. roots.Where(root => !volumes.Any(volume => IsUnder(disk.Real(root), volume)))];
    }

    /// <summary>The file as this server will read it, or why it may not be read.</summary>
    /// <remarks>
    /// Returns the resolved path rather than a yes or no, because every caller needs the resolved one anyway,
    /// and resolving it twice is how the check and the read drift apart.
    /// </remarks>
    public Reached Reach(string? path)
    {
        var roots = All;

        if (roots.Count == 0)
            return Reached.No("this session was given no root, so no file is reachable",
                "open the session at …?root=/path/to/the/copy, or set SNAIL_MCP_DOCUMENTS_ROOTS__0");

        if (string.IsNullOrWhiteSpace(path))
            return Reached.No("name a file", $"paths are relative to {roots[0]}, or absolute inside it");

        string full = disk.Real(Path.IsPathRooted(path.Trim())
            ? Path.GetFullPath(path.Trim())
            : Path.GetFullPath(Path.Combine(roots[0], path.Trim())));

        if (!roots.Any(root => IsUnder(full, disk.Real(root))))
            return Reached.No($"'{path.Trim()}' is outside this session, which reads {string.Join(", ", roots)}");

        return disk.IsFile(full)
            ? Reached.Yes(full)
            : Reached.No($"there is no file '{path.Trim()}'", disk.IsFolder(full) ? "that is a folder: name a file inside it" : "check the name and the folder it is in");
    }

    private static IReadOnlyList<string> Paths(IEnumerable<string> roots) =>
    [
        .. roots
            .Where(root => !string.IsNullOrWhiteSpace(root))
            .Select(root => Path.GetFullPath(root.Trim()))
            .Distinct(StringComparer.Ordinal)
    ];

    private static bool IsUnder(string path, string root) =>
        string.Equals(path, root, StringComparison.Ordinal)
        || path.StartsWith($"{root.TrimEnd(Path.DirectorySeparatorChar)}{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
}
