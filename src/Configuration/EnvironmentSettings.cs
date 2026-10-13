namespace Snail.MCP.Documents.Configuration;

/// <summary>Maps <c>SNAIL_MCP_DOCUMENTS_FIELD_NAME</c> variables onto configuration keys <c>FieldName</c>.</summary>
/// <remarks>
/// The platform hands a child process only variables under <c>SNAIL_MCP_</c>, so a setting spelled any other
/// way would never arrive. A double underscore nests: <c>SNAIL_MCP_DOCUMENTS_ROOTS__0</c> is <c>Roots:0</c>.
/// </remarks>
public static class EnvironmentSettings
{
    /// <summary>What every variable this server reads begins with.</summary>
    public const string Prefix = "SNAIL_MCP_DOCUMENTS_";

    /// <summary>The prefixed variables as configuration keys, blank ones left out; the reading is Snail.Sdk.Mcp.Hosting's.</summary>
    public static IEnumerable<KeyValuePair<string, string?>> Read(System.Collections.IDictionary variables) =>
        Snail.Sdk.Mcp.Hosting.EnvironmentSettings.Read(Prefix, variables);
}
