using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Snail.MCP.Documents.Application;
using Snail.MCP.Documents.Tools.Prompts;

namespace Snail.MCP.Documents.Tools;

/// <summary>What the documents of the copy say: an outline to find a place by, the text in pieces, a table row by row.</summary>
[McpServerToolType]
public sealed class DocumentTools(DocumentReading reading, DocumentOutline outline, TableReading tables)
{
    /// <summary>Reads a document as Markdown, a piece at a time.</summary>
    [McpServerTool(Name = "documents_read", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description(ToolDescriptions.Read)]
    public async Task<CallToolResult> ReadAsync(
        [Description(ToolDescriptions.ReadPath)] string path,
        [Description(ToolDescriptions.ReadFrom)] int from = 0,
        [Description(ToolDescriptions.ReadPart)] int? part = null,
        [Description(ToolDescriptions.ReadCharacters)] int maxCharacters = Limits.DefaultCharacters,
        CancellationToken cancellationToken = default) =>
        ToolResponse.From(await reading.ReadAsync(path, from, part, maxCharacters, cancellationToken));

    /// <summary>Says what a document holds without reading it.</summary>
    [McpServerTool(Name = "documents_outline", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description(ToolDescriptions.Outline)]
    public async Task<CallToolResult> OutlineAsync(
        [Description(ToolDescriptions.OutlinePath)] string path,
        CancellationToken cancellationToken = default) =>
        ToolResponse.From(await outline.OutlineAsync(path, cancellationToken));

    /// <summary>Reads one table of a document as rows of cells.</summary>
    [McpServerTool(Name = "documents_table", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description(ToolDescriptions.Table)]
    public async Task<CallToolResult> TableAsync(
        [Description(ToolDescriptions.TablePath)] string path,
        [Description(ToolDescriptions.TableNumber)] int table = 1,
        [Description(ToolDescriptions.TableFromRow)] int fromRow = 0,
        [Description(ToolDescriptions.TableRows)] int maxRows = Limits.DefaultRows,
        CancellationToken cancellationToken = default) =>
        ToolResponse.From(await tables.ReadAsync(path, table, fromRow, maxRows, cancellationToken));
}
