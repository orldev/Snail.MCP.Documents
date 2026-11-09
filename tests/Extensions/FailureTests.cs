using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Snail.MCP.Documents.Application.Ports;
using Snail.MCP.Documents.Configuration;
using Snail.MCP.Documents.Domain;
using Snail.MCP.Documents.Extensions;
using Snail.Sdk.Mcp.Http;
using Snail.Sdk.Mcp.Testing;

namespace Snail.MCP.Documents.Tests.Extensions;

/// <summary>A call that failed inside the server for a reason nobody foresaw, as the model meets it.</summary>
public sealed class FailureTests : IDisposable
{
    private readonly Folder _copy = new();

    public void Dispose() => _copy.Dispose();

    /// <summary>
    /// The failure names itself — the tool, the kind of failure and its message — and says what to do: the fault is the
    /// server's and the file's, not the call's, so the model reports the file rather than repeating the call that will
    /// fail the same way.
    /// </summary>
    [Fact]
    public async Task Call_ThatFailsInsideTheServer_NamesTheToolAndAsksToReportTheFile()
    {
        _copy.Write("a.txt", "x");
        await using var server = await InProcessServer.StartAsync(Configured, Mapped);
        await using var session = await server.SessionAsync($"root={Uri.EscapeDataString(_copy.Root)}");

        var answer = await session.CallToolAsync("documents_read", new Dictionary<string, object?> { ["path"] = "a.txt" });

        Assert.Equal(true, answer.IsError);
        Assert.Equal("documents_read failed inside the server (InvalidOperationException): the disk lost its place", answer.Error());
        Assert.Contains("report the file", answer.Hint(), StringComparison.Ordinal);
        Assert.Equal("internal", answer.Type());
    }

    private void Configured(WebApplicationBuilder builder)
    {
        builder.ConfigureMcpServerAtAnAddress(new ServerConfig { Roots = [_copy.Root] });
        builder.Services.AddSingleton<IDisk, BrokenDisk>();
    }

    private static void Mapped(WebApplication app)
    {
        app.UseSessionRoots(TransportRegistrationExtensions.Volumes);
        app.MapLocalMcp();
    }

    /// <summary>A disk that fails in a way no use case answers, as a fault of the server would.</summary>
    private sealed class BrokenDisk : IDisk
    {
        public FileStamp Stamp(string path) => throw new InvalidOperationException("the disk lost its place");

        public Task<Stream> ReadAsync(string path, int most, CancellationToken cancellationToken) => throw new InvalidOperationException("the disk lost its place");
    }
}
