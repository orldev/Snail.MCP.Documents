using System.Diagnostics;
using Snail.MCP.Documents.Adapters;

namespace Snail.MCP.Documents.Tests.Adapters;

/// <summary>How the file system is read: only regular files, never through a link, never past the size it may have.</summary>
public sealed class LocalDiskTests : IDisposable
{
    private readonly Folder _copy = new();

    public void Dispose() => _copy.Dispose();

    [Fact]
    public async Task ReadAsync_AFileOfTheCopy_IsItsBytes()
    {
        string file = _copy.Write("a.txt", "текст");

        await using var content = await new LocalDisk().ReadAsync(new LocalDisk().Real(file), 1_024, CancellationToken.None);
        using var text = new StreamReader(content);

        Assert.Equal("текст", await text.ReadToEndAsync());
    }

    /// <summary>The path handed over is resolved already; a link on it now is one swapped in since, and following it reads whatever it leads to.</summary>
    [Fact]
    public async Task ReadAsync_APathThatLeadsThroughALink_IsRefused()
    {
        if (OperatingSystem.IsWindows())
            return;

        string target = _copy.Write("target.txt", "x");
        string link = Path.Combine(new LocalDisk().Real(_copy.Root), "link.txt");
        File.CreateSymbolicLink(link, target);

        await Assert.ThrowsAsync<IOException>(() => new LocalDisk().ReadAsync(link, 1_024, CancellationToken.None));
    }

    /// <summary>A link swapped in higher up the path leads just as far; refusing the last component alone would not see it.</summary>
    [Fact]
    public async Task ReadAsync_APathThroughALinkedFolder_IsRefused()
    {
        if (OperatingSystem.IsWindows())
            return;

        _copy.Write("real/a.txt", "x");
        string root = new LocalDisk().Real(_copy.Root);
        Directory.CreateSymbolicLink(Path.Combine(root, "linked"), Path.Combine(root, "real"));

        await Assert.ThrowsAsync<IOException>(() => new LocalDisk().ReadAsync(Path.Combine(root, "linked", "a.txt"), 1_024, CancellationToken.None));
    }

    /// <summary>A named pipe passes for a file of no bytes; opening it the ordinary way waits for a writer that never comes.</summary>
    [Fact]
    public async Task ReadAsync_ANamedPipe_IsRefusedWithoutWaiting()
    {
        if (OperatingSystem.IsWindows())
            return;

        string pipe = Path.Combine(new LocalDisk().Real(_copy.Root), "pipe.txt");
        using (var made = Process.Start("mkfifo", pipe))
            await made.WaitForExitAsync();

        var read = new LocalDisk().ReadAsync(pipe, 1_024, CancellationToken.None);

        var failure = await Assert.ThrowsAsync<IOException>(() => read.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Contains("not a regular file", failure.Message, StringComparison.Ordinal);
    }

    /// <summary>Every path ends at a root; on Windows asking <c>C:\</c> whether it is a link threw, and no file was read.</summary>
    [Fact]
    public void Real_TheRootOfTheDisk_IsItself()
    {
        string root = Path.GetPathRoot(Path.GetTempPath())!;

        Assert.Equal(root, new LocalDisk().Real(root));
    }

    [Fact]
    public async Task ReadAsync_AFileLargerThanItMayBe_IsRefusedAsTooLarge()
    {
        string file = _copy.Write("big.txt", new string('x', 4_096));

        await Assert.ThrowsAsync<DocumentTooLargeException>(() => new LocalDisk().ReadAsync(new LocalDisk().Real(file), 1_024, CancellationToken.None));
    }
}
