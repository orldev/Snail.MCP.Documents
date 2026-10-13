using Snail.MCP.Documents.Configuration;

namespace Snail.MCP.Documents.Tests.Configuration;

public sealed class ServerConfigTests
{
    [Fact]
    public void Read_WithNothingSet_SpeaksStdioAndReadsNothing()
    {
        var config = ServerConfigExtensions.Read(Deployment.Settings());

        Assert.Equal(0, config.Port);
        Assert.Empty(config.Roots);
    }

    [Fact]
    public void Read_PrefixedVariables_LandOnTheirSettings()
    {
        var config = ServerConfigExtensions.Read(Deployment.Settings(("PORT", "8823"), ("ROOTS__0", "/workspaces"), ("MAX_FILE_BYTES", "2048")));

        Assert.Equal(8823, config.Port);
        Assert.Equal(["/workspaces"], config.Roots);
        Assert.Equal(2048, config.MaxFileBytes);
    }

    /// <summary>The platform hands a child only <c>SNAIL_MCP_</c> variables; anything else is somebody else's setting.</summary>
    [Fact]
    public void Read_AnUnprefixedVariable_IsNotASettingOfThisServer()
    {
        var settings = ServerConfigExtensions.Settings(null, new Dictionary<string, string> { ["PORT"] = "1" });

        Assert.Equal(0, ServerConfigExtensions.Read(settings).Port);
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
    [InlineData("PORT", "70000")]
    [InlineData("MAX_FILE_BYTES", "10")]
    [InlineData("MEMORY_MINUTES", "500")]
    [InlineData("MEMORY_DOCUMENTS", "0")]
    [InlineData("ROOTS__0", "relative/path")]
    public void Read_ASettingOutOfRange_StopsTheProcess(string name, string value) =>
        Assert.Throws<InvalidOperationException>(() => ServerConfigExtensions.Read(Deployment.Settings((name, value))));
}
