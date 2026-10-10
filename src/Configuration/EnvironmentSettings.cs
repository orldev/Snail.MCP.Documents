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

    /// <summary>The prefixed variables as configuration keys, blank ones left out.</summary>
    public static IEnumerable<KeyValuePair<string, string?>> Read(System.Collections.IDictionary variables)
    {
        foreach (System.Collections.DictionaryEntry entry in variables)
        {
            if (entry.Key is not string name || !name.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            if (entry.Value is not string value || string.IsNullOrWhiteSpace(value))
                continue;

            yield return new KeyValuePair<string, string?>(ToConfigurationKey(name[Prefix.Length..]), value.Trim());
        }
    }

    internal static string ToConfigurationKey(string variable) =>
        string.Join(':', variable.Split("__").Select(segment => segment.Replace("_", string.Empty, StringComparison.Ordinal)));
}
