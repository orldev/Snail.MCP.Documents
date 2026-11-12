using Snail.MCP.Documents.Configuration;
using Snail.MCP.Documents.Extensions;
using Snail.Sdk.Mcp.Hosting;

namespace Snail.MCP.Documents.Tests.Configuration;

public sealed class ServerConfigTests
{
    [Fact]
    public void Read_WithNothingSet_SpeaksStdioAndReadsNothing()
    {
        var settings = Deployment.Settings();

        Assert.Equal(McpTransport.Stdio, McpServerHost.Endpoint(settings).Transport);
        Assert.Empty(ServerConfigExtensions.Read(settings).Roots);
    }

    [Fact]
    public void Read_PrefixedVariables_LandOnTheirSettings()
    {
        var settings = Deployment.Settings(("PORT", "8823"), ("ROOTS__0", "/workspaces"), ("MAX_FILE_BYTES", "2048"));
        var config = ServerConfigExtensions.Read(settings);
        var endpoint = McpServerHost.Endpoint(settings);

        Assert.Equal(McpTransport.Http, endpoint.Transport);
        Assert.Equal(8823, endpoint.Url.Port);
        Assert.Equal(["/workspaces"], config.Roots);
        Assert.Equal(2048, config.MaxFileBytes);
    }

    /// <summary>The platform hands a child only <c>SNAIL_MCP_</c> variables; anything else is somebody else's setting.</summary>
    [Fact]
    public void Read_AnUnprefixedVariable_IsNotASettingOfThisServer()
    {
        var settings = ServerConfigExtensions.Settings(null, new Dictionary<string, string> { ["PORT"] = "1" });

        Assert.Equal(McpTransport.Stdio, McpServerHost.Endpoint(settings).Transport);
    }

    [Fact]
    public void Read_ASettingsFile_LandsOnItsSettings()
    {
        using var folder = new Folder();
        string file = folder.Write("config.json", """{ "Roots": ["/workspaces"], "MemoryMinutes": 30 }""");

        var config = ServerConfigExtensions.Read(ServerConfigExtensions.Settings(file, Deployment.Variables()));

        Assert.Equal(["/workspaces"], config.Roots);
        Assert.Equal(30, config.MemoryMinutes);
    }

    /// <summary>A variable is what a deployment says for this one run, so it overrules the file a person keeps.</summary>
    [Fact]
    public void Read_AVariableAndTheSettingsFile_TheVariableWins()
    {
        using var folder = new Folder();
        string file = folder.Write("config.json", """{ "MemoryMinutes": 30, "MemoryDocuments": 4 }""");

        var config = ServerConfigExtensions.Read(ServerConfigExtensions.Settings(file, Deployment.Variables(("MEMORY_MINUTES", "5"))));

        Assert.Equal(5, config.MemoryMinutes);
        Assert.Equal(4, config.MemoryDocuments);
    }

    [Fact]
    public void Read_ASettingsFileThatIsNotThere_LeavesTheDefaults()
    {
        var config = ServerConfigExtensions.Read(ServerConfigExtensions.Settings(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json"), Deployment.Variables()));

        Assert.Equal(new ServerConfig().MemoryMinutes, config.MemoryMinutes);
    }

    [Theory]
    [InlineData("MAX_FILE_BYTES", "10")]
    [InlineData("MEMORY_MINUTES", "500")]
    [InlineData("MEMORY_DOCUMENTS", "0")]
    [InlineData("ROOTS__0", "relative/path")]
    public void Read_ASettingOutOfRange_StopsTheProcessNamingTheVariable(string name, string value)
    {
        var refusal = Assert.Throws<StartRefusedException>(() => ServerConfigExtensions.Read(Deployment.Settings((name, value))));

        Assert.Contains($"SNAIL_MCP_DOCUMENTS_{name} (or ", refusal.Message, StringComparison.Ordinal);
        Assert.Contains(value, refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_FileBytesOutOfRange_NamesTheVariableAndTheKeyOfTheSettingsFile()
    {
        var refusal = Assert.Throws<StartRefusedException>(() => ServerConfigExtensions.Read(Deployment.Settings(("MAX_FILE_BYTES", "10"))));

        Assert.Equal(
            "invalid configuration: SNAIL_MCP_DOCUMENTS_MAX_FILE_BYTES (or MaxFileBytes in the settings file) must be between 1024 and 536870912, not 10",
            refusal.Message);
    }

    /// <summary>The address is one of the server's settings: a settings file that nests them under <c>Snail</c> is heard for it too.</summary>
    [Fact]
    public void Endpoint_APortInTheSnailSectionOfTheSettingsFile_ListensOnIt()
    {
        using var folder = new Folder();
        string file = folder.Write("config.json", """{ "Snail": { "Port": 8824, "Roots": ["/workspaces"] } }""");
        var settings = ServerConfigExtensions.Settings(file, Deployment.Variables());

        var endpoint = McpServerHost.Endpoint(settings);

        Assert.Equal(McpTransport.Http, endpoint.Transport);
        Assert.Equal(8824, endpoint.Url.Port);
        Assert.Equal(["/workspaces"], ServerConfigExtensions.Read(settings).Roots);
    }

    /// <summary>With no token the network is the access control: outside a container the server stays on loopback, and a setting that would open every interface stops the start, naming the variable that asked for it.</summary>
    [OutsideAContainerTheory]
    [InlineData("LISTENS_ON_EVERY_INTERFACE", "true")]
    [InlineData("URL", "http://0.0.0.0:8823/")]
    [InlineData("URL", "http://192.168.1.10:8823/")]
    public void Endpoint_BeyondLoopbackOutsideAContainer_StopsTheProcess(string name, string value)
    {
        var refusal = Assert.Throws<StartRefusedException>(() => McpServerHost.Endpoint(Deployment.Settings(("TRANSPORT", "Http"), (name, value))));

        Assert.Contains($"SNAIL_MCP_DOCUMENTS_{name}", refusal.Message, StringComparison.Ordinal);
    }

    /// <summary>A port no socket can have stops the start, as every other setting out of its range does, naming its variable.</summary>
    [Fact]
    public void Endpoint_APortOutOfRange_StopsTheProcess()
    {
        var refusal = Assert.Throws<StartRefusedException>(() => McpServerHost.Endpoint(Deployment.Settings(("PORT", "70000"))));

        Assert.Contains("SNAIL_MCP_DOCUMENTS_PORT (or Port in the settings file) is '70000'", refusal.Message, StringComparison.Ordinal);
    }
}
