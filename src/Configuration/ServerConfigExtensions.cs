using Microsoft.Extensions.Configuration;

namespace Snail.MCP.Documents.Configuration;

/// <summary>Builds the settings the server runs on from the environment alone.</summary>
public static class ServerConfigExtensions
{
    /// <summary>Everything configured for this server.</summary>
    /// <remarks>
    /// Built apart from the host's own configuration on purpose: an <c>appsettings.json</c> lying in the
    /// working directory of whoever started the server, or an unprefixed variable of theirs, is not a
    /// setting of this server and must not become one by accident.
    /// </remarks>
    public static IConfiguration Settings(System.Collections.IDictionary? variables = null) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(EnvironmentSettings.Read(variables ?? Environment.GetEnvironmentVariables()))
            .Build();

    /// <summary>What the settings say the server is, read before there is a host to read it into.</summary>
    /// <remarks>
    /// Read first and separately because the answer decides which host is built at all: a port means a
    /// web host listening for sessions, no port means a console speaking stdio to its parent.
    /// </remarks>
    public static ServerConfig Read(IConfiguration settings)
    {
        var config = new ServerConfig();

        settings.GetSection(ServerConfig.SectionName).Bind(config);
        settings.Bind(config);

        Validate(config);

        return config;
    }

    /// <summary>Refuses a setting outside its range at start-up, so a typo stops the process instead of being repaired in silence.</summary>
    internal static void Validate(ServerConfig config)
    {
        List<string> faults = [];

        if (config.Port is < 0 or > 65535)
            faults.Add($"Port must be between 0 and 65535, not {config.Port}");

        if (config.MaxFileBytes is < 1024 or > 536_870_912)
            faults.Add($"MaxFileBytes must be between 1024 and 536870912, not {config.MaxFileBytes}");

        if (config.MemoryMinutes is < 0 or > 120)
            faults.Add($"MemoryMinutes must be between 0 and 120, not {config.MemoryMinutes}");

        if (config.MemoryDocuments is < 1 or > 128)
            faults.Add($"MemoryDocuments must be between 1 and 128, not {config.MemoryDocuments}");

        if (config.Roots.FirstOrDefault(root => !string.IsNullOrWhiteSpace(root) && !Path.IsPathRooted(root.Trim())) is { } relative)
            faults.Add($"Roots must be absolute paths, and '{relative}' is not");

        if (faults.Count > 0)
            throw new InvalidOperationException($"invalid configuration: {string.Join("; ", faults)}");
    }
}
