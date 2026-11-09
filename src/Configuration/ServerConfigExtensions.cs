using System.Collections;
using Microsoft.Extensions.Configuration;
using Snail.Sdk.Mcp.Hosting;

namespace Snail.MCP.Documents.Configuration;

/// <summary>Builds the settings the server runs on from its settings file and its variables.</summary>
public static class ServerConfigExtensions
{
    /// <summary>The server's short name: its settings file is <c>~/.config/s3l-mcp-documents/config.json</c>, its variables begin with <c>SNAIL_MCP_DOCUMENTS_</c>.</summary>
    public const string Name = "documents";

    /// <summary>Everything configured for this server: the first settings file of Snail.Sdk's chain, then the variables, which win.</summary>
    /// <remarks>
    /// Built apart from the host's own configuration on purpose: an <c>appsettings.json</c> lying in the
    /// working directory of whoever started the server, or an unprefixed variable of theirs, is not a
    /// setting of this server and must not become one by accident.
    /// </remarks>
    public static IConfiguration Settings() => Settings(ServerSettings.FileOf(Name), Environment.GetEnvironmentVariables());

    /// <summary>The settings of <paramref name="file"/>, when there is one, under the server's variables among <paramref name="variables"/>.</summary>
    public static IConfiguration Settings(string? file, IDictionary variables) =>
        new ConfigurationBuilder()
            .AddServerSettings(Name, file, variables)
            .Build();

    /// <summary>What the settings say the server may read, read before there is a host to read it into.</summary>
    /// <remarks>
    /// Read first and separately because a setting out of its range stops the process before any host is built.
    /// </remarks>
    public static ServerConfig Read(IConfiguration settings)
    {
        var config = new ServerConfig();

        ServerSettings.Bind(Name, settings, source =>
        {
            source.GetSection(ServerConfig.SectionName).Bind(config);
            source.Bind(config);
        });

        Validate(config);

        return config;
    }

    /// <summary>Refuses a setting outside its range at start-up, so a typo stops the process instead of being repaired in silence.</summary>
    /// <remarks>
    /// As Snail.Sdk's <see cref="StartRefusedException"/>, which the program says in one line with exit code 1 rather than
    /// crashing with a trace; each fault names its setting as <c>McpEndpoint</c> names its own, the variable and the key
    /// of the settings file, so the person who set it finds where.
    /// </remarks>
    internal static void Validate(ServerConfig config)
    {
        List<string> faults = [];

        if (config.MaxFileBytes is < 1024 or > 536_870_912)
            faults.Add($"{SettingOf(nameof(ServerConfig.MaxFileBytes))} must be between 1024 and 536870912, not {config.MaxFileBytes}");

        if (config.MemoryMinutes is < 0 or > 120)
            faults.Add($"{SettingOf(nameof(ServerConfig.MemoryMinutes))} must be between 0 and 120, not {config.MemoryMinutes}");

        if (config.MemoryDocuments is < 1 or > 128)
            faults.Add($"{SettingOf(nameof(ServerConfig.MemoryDocuments))} must be between 1 and 128, not {config.MemoryDocuments}");

        var relative = Array.FindIndex(config.Roots, root => !string.IsNullOrWhiteSpace(root) && !Path.IsPathRooted(root.Trim()));

        if (relative >= 0)
            faults.Add($"{SettingOf($"{nameof(ServerConfig.Roots)}:{relative}")} must be an absolute path, and '{config.Roots[relative]}' is not");

        if (faults.Count > 0)
            throw new StartRefusedException($"invalid configuration: {string.Join("; ", faults)}");
    }

    /// <summary><c>SNAIL_MCP_DOCUMENTS_MAX_FILE_BYTES (or MaxFileBytes in the settings file)</c> for <c>MaxFileBytes</c>.</summary>
    private static string SettingOf(string key) => ServerSettings.NameOfSetting(Name, key);
}
