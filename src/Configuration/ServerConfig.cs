namespace Snail.MCP.Documents.Configuration;

/// <summary>Everything the server is allowed to read, and how much of it.</summary>
/// <remarks>
/// The roots are the whole security model: a reader of files is one call away from every file its process
/// can open, so it refuses every path that is not inside one of them. A session states its own root in the
/// address it opens — that is what lets one server stand behind every worker of a machine — and these stand
/// when it states none, which is how a server started as a program for one caller is confined.
/// </remarks>
public sealed class ServerConfig
{
    /// <summary>The section a settings file may nest these under.</summary>
    public const string SectionName = "Snail";

    /// <summary>Port to answer on; none means the server is started as a program and speaks stdio.</summary>
    /// <remarks>
    /// One knob decides the whole shape of the process, because the two shapes serve different callers:
    /// at an address it stands behind every worker of a machine and each session names its own copy, as a
    /// program it belongs to one caller and is confined by <see cref="Roots"/>.
    /// </remarks>
    public int Port { get; set; }

    /// <summary>Absolute paths the agent may read documents from; nothing outside them is reachable.</summary>
    public string[] Roots { get; set; } = [];

    /// <summary>Bytes past which a file is not opened at all.</summary>
    /// <remarks>
    /// A document is read whole before a piece of it is cut, and a spreadsheet of a gigabyte would hold the
    /// server's memory for every session on the machine; 50 MB is a long report with its pictures.
    /// </remarks>
    public int MaxFileBytes { get; set; } = 52_428_800;

    /// <summary>Minutes a document that was read is remembered, so its next piece is served without reading it again; 0 forgets at once.</summary>
    public int MemoryMinutes { get; set; } = 10;

    /// <summary>Documents remembered at once; the one read longest ago gives way.</summary>
    public int MemoryDocuments { get; set; } = 8;
}
