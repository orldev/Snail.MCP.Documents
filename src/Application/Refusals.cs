using Snail.Sdk.Mcp.Hosting.Sessions;

namespace Snail.MCP.Documents.Application;

/// <summary>The kinds of refusal this server answers with, the top-level <c>type</c> of a failed answer a program branches on.</summary>
/// <remarks>
/// The SDK's words where one fits — <c>arguments</c>, <c>unreadable</c>, <c>timeout</c> — and a word of this server's own,
/// one per kind of failure, where none does. A path the session may not use keeps the kind <see cref="SessionRoots"/> gave it
/// (<c>outside</c>, <c>absent</c>, <c>notAFile</c> and the rest), and a failure nobody foresaw stays <c>internal</c>.
/// </remarks>
public static class Refusals
{
    /// <summary>A number outside the range its parameter takes, in the word the SDK's argument check answers a parameter it could not take with.</summary>
    public const string Arguments = "arguments";

    /// <summary>A file that is there and could not be opened, in the word <see cref="Reached.Unreadable"/> uses for a disk that failed.</summary>
    public const string Unreadable = Reached.Unreadable;

    /// <summary>A file read for longer than a file may, in the word Snail.Sdk.Mcp.Programs uses for a program stopped by its deadline.</summary>
    public const string Timeout = "timeout";

    /// <summary>A file over the size limit on disk, or one that comes to too much once read.</summary>
    public const string TooLarge = "tooLarge";

    /// <summary>A file of a format nobody here reads.</summary>
    public const string UnknownFormat = "unknownFormat";

    /// <summary>A file that claims a format and is not in it.</summary>
    public const string Damaged = "damaged";

    /// <summary>A file protected by a password.</summary>
    public const string Locked = "locked";

    /// <summary>A page, sheet or slide the document does not have, in the shape of the SDK's <c>noSuchTool</c>.</summary>
    public const string NoSuchPart = "noSuchPart";

    /// <summary>A table the document does not have.</summary>
    public const string NoSuchTable = "noSuchTable";

    /// <summary>A position or a row past the end of the text or the table: everything there is has been read.</summary>
    public const string PastTheEnd = "pastTheEnd";
}
