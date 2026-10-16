namespace Snail.MCP.Documents.Domain;

/// <summary>What tells one version of a file from the next: its size and the moment it was last written.</summary>
public readonly record struct FileStamp(long Length, DateTime WrittenUtc);
