namespace Snail.MCP.Documents.Tests.Support;

/// <summary>A working copy of the test's own, on disk and gone when the test ends.</summary>
internal sealed class Folder : IDisposable
{
    public string Root { get; } = Directory.CreateTempSubdirectory("snail-documents-").FullName;

    public string Write(string path, string text)
    {
        string full = Path.Combine(Root, path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, text);

        return full;
    }

    public string Write(string path, byte[] bytes)
    {
        string full = Path.Combine(Root, path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllBytes(full, bytes);

        return full;
    }

    public void Dispose() => Directory.Delete(Root, recursive: true);
}
