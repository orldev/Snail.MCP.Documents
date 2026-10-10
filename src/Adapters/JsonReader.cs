using System.Text.Encodings.Web;
using System.Text.Json;

namespace Snail.MCP.Documents.Adapters;

/// <summary>A JSON file: a list of flat records as a table, anything else as indented JSON.</summary>
/// <remarks>
/// A list of records is what an export usually is, and a table of it is what a model counts and compares in;
/// any other shape is kept as JSON, indented, because flattening a tree loses which value belongs to what.
/// </remarks>
public sealed class JsonReader : IFormatReader
{
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <inheritdoc/>
    public DocumentKind Format => DocumentKind.Json;

    /// <inheritdoc/>
    public IReadOnlyList<string> Extensions { get; } = [".json"];

    /// <inheritdoc/>
    public IReadOnlyList<Block> Read(Stream content)
    {
        JsonNode? root;

        try
        {
            root = JsonNode.Parse(Decoding.Text(content));
        }
        catch (JsonException failure)
        {
            throw new InvalidDataException(failure.Message, failure);
        }

        if (root is JsonArray { Count: > 0 } records && records.All(IsFlatRecord))
            return [new TableBlock(null, Table(records))];

        return [new CodeBlock("json", root?.ToJsonString(Indented) ?? "null")];
    }

    private static bool IsFlatRecord(JsonNode? node) =>
        node is JsonObject record && record.All(field => field.Value is null or JsonValue);

    private static IReadOnlyList<IReadOnlyList<string>> Table(JsonArray records)
    {
        List<string> columns = [.. records.Cast<JsonObject>().SelectMany(record => record.Select(field => field.Key)).Distinct(StringComparer.Ordinal)];

        return
        [
            columns,
            .. records.Cast<JsonObject>().Select(record => (IReadOnlyList<string>)[.. columns.Select(column => Value(record[column]))])
        ];
    }

    private static string Value(JsonNode? value) => value switch
    {
        null => string.Empty,
        JsonValue text when text.GetValueKind() == JsonValueKind.String => text.GetValue<string>(),
        _ => value.ToJsonString()
    };
}
