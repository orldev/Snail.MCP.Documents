using Snail.Sdk.Mcp.Testing;

namespace Snail.MCP.Documents.Tests.Support;

/// <summary>A working copy of the test's own, on disk and gone when the test ends.</summary>
/// <remarks>
/// Made by <see cref="TestFolder.Create"/>, named after the test process and the second it started, so that a run that
/// crashes leaves a folder a cleaner can prove is a leftover instead of one named at random.
/// </remarks>
internal sealed class Folder : IDisposable
{
    public string Root { get; } = TestFolder.Create();

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
