using System.Text.RegularExpressions;
using System.Xml.Linq;
using Snail.MCP.Documents.Configuration;
using Snail.MCP.Documents.Extensions;
using Snail.Sdk.Mcp.Hosting;

namespace Snail.MCP.Documents.Tests.Contracts;

/// <summary>
/// The manifest a registry shows, the entry the platform's catalog takes, the image's labels and the settings the
/// code binds say the same thing. All four are written by hand, and none of them is compiled against the others.
/// </summary>
public sealed partial class RegistryManifestContractTests
{
    private static readonly JsonNode Manifest = JsonNode.Parse(Repository.Read(".mcp", "server.json"))!;

    private static readonly JsonNode Entry = JsonNode.Parse(Repository.Read("deploy", "swarm", "catalog-entry.json"))!;

    private static IEnumerable<JsonNode> Variables => Manifest["packages"]![0]!["environmentVariables"]!.AsArray()!;

    /// <summary>A setting the code reads and the manifest never names is one no operator will find.</summary>
    [Fact]
    public void Manifest_NamesEverySettingOfTheServer_WithTheDefaultTheCodeStartsFrom()
    {
        var config = new ServerConfig();

        Assert.All(typeof(ServerConfig).GetProperties(), setting =>
        {
            string variable = $"{ServerSettings.PrefixOf(ServerConfigExtensions.Name)}{Screaming().Replace(setting.Name, "_$1").ToUpperInvariant()}";
            var named = Variables.SingleOrDefault(candidate => candidate["name"]!.GetValue<string>() == variable);

            Assert.True(named is not null, variable);

            if (setting.PropertyType.IsArray)
                Assert.Null(named["default"]);
            else
                Assert.Equal(Convert.ToString(setting.GetValue(config), System.Globalization.CultureInfo.InvariantCulture), named["default"]!.GetValue<string>(), ignoreCase: true);
        });
    }

    [Fact]
    public void Manifest_CarriesTheVersion_OfThePackageItDescribes()
    {
        string version = XDocument.Parse(Repository.Read("src", "Snail.MCP.Documents.csproj")).Descendants("Version").Single().Value;

        Assert.Equal(version, Manifest["version"]!.GetValue<string>());
        Assert.Equal(version, Manifest["packages"]![0]!["version"]!.GetValue<string>());
        Assert.EndsWith($":{version}", Entry["image"]!.GetValue<string>(), StringComparison.Ordinal);
    }

    /// <summary>The platform computes the address from this entry; a port or a path that differs from the image is a server nobody reaches.</summary>
    [Fact]
    public void CatalogEntry_AgreesWithTheImage_OnThePortAndThePath()
    {
        string dockerfile = Repository.Read("Dockerfile");

        Assert.Contains($"mcp.port=\"{Entry["port"]!.GetValue<int>()}\"", dockerfile, StringComparison.Ordinal);
        Assert.Contains($"mcp.path=\"{Entry["path"]!.GetValue<string>()}\"", dockerfile, StringComparison.Ordinal);
        Assert.Contains($"SNAIL_MCP_DOCUMENTS_PORT={Entry["port"]!.GetValue<int>()}", dockerfile, StringComparison.Ordinal);
        Assert.StartsWith(McpServerHost.Path, Entry["path"]!.GetValue<string>(), StringComparison.Ordinal);
    }

    /// <summary>The server reads documents and has no business writing them: the copies are mounted read-only wherever it stands.</summary>
    [Fact]
    public void PlatformComposeBlock_MountsTheWorkingCopiesReadOnly()
    {
        string compose = Repository.Read("deploy", "swarm", "compose.yml");

        Assert.Contains("worker-workspaces:/workspaces:ro", compose, StringComparison.Ordinal);
        Assert.Contains($"SNAIL_MCP_DOCUMENTS_PORT: {Entry["port"]!.GetValue<int>()}", compose, StringComparison.Ordinal);
        Assert.Contains("  mcp-documents:", compose, StringComparison.Ordinal);
    }

    [GeneratedRegex("(?<!^)([A-Z])", RegexOptions.CultureInvariant)]
    private static partial Regex Screaming();
}
