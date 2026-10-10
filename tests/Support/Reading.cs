using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Snail.MCP.Documents.Adapters;
using Snail.MCP.Documents.Application;
using Snail.MCP.Documents.Configuration;
using Snail.MCP.Documents.Extensions;

namespace Snail.MCP.Documents.Tests.Support;

/// <summary>The server's use cases wired as the host wires them, confined to one folder, with no transport in front.</summary>
internal sealed class Reading : IDisposable
{
    private readonly ServiceProvider _services;

    private readonly IServiceScope _scope;

    public Reading(string root, int maxFileBytes = 52_428_800)
    {
        _services = new ServiceCollection()
            .AddSingleton(new ServerConfig { Roots = [root], MaxFileBytes = maxFileBytes })
            .AddSingleton<Snail.MCP.Documents.Application.Ports.ISessionAddress, StdioAddress>()
            .AddReading(new ConfigurationBuilder().Build())
            .BuildServiceProvider();

        _scope = _services.CreateScope();
    }

    public DocumentReading Documents => _scope.ServiceProvider.GetRequiredService<DocumentReading>();

    public DocumentOutline Outline => _scope.ServiceProvider.GetRequiredService<DocumentOutline>();

    public TableReading Tables => _scope.ServiceProvider.GetRequiredService<TableReading>();

    public void Dispose()
    {
        _scope.Dispose();
        _services.Dispose();
    }
}
