using Snail.MCP.Documents.Tools.Prompts;

namespace Snail.MCP.Documents.Application;

/// <summary>Answers «what does this document say»: its text as Markdown, a piece at a time.</summary>
/// <remarks>
/// A report of three hundred pages must not land in a model's context whole, and a document cut without a word
/// reads as one that ended. So a piece says where it stands in the whole — how much there is, how much of it
/// this is, which pages it covers — and the position the next piece starts at.
/// </remarks>
public sealed class DocumentReading(DocumentOpening opening)
{
    /// <summary>The text of the document from <paramref name="from"/>, or from the start of part <paramref name="part"/> when one is named.</summary>
    public async Task<Answer> ReadAsync(string path, int from, int? part, int maxCharacters, CancellationToken cancellationToken = default)
    {
        if (Refusal(from, part, maxCharacters) is { } refusal)
            return Answer.Failed(refusal);

        var opened = await opening.OpenAsync(path, cancellationToken);
        if (opened.Document is not { } document)
            return opened.Failure;

        if (part is { } number && (number > document.Parts.Count))
            return Answer.Failed(Messages.NoSuchPart(number, document.Parts.Count), Messages.OutlineHint);

        int start = part is { } wanted ? document.Parts[wanted - 1].Start : from;

        return start > 0 && start >= document.Text.Length
            ? Answer.Failed(Messages.FromBeyondTheEnd(start, document.Text.Length))
            : Answer.Ok(Described(path, document, start, maxCharacters));
    }

    /// <summary>The piece as a caller reads it: what the document is, how much of it this is, the parts it covers and the text.</summary>
    internal static JsonObject Described(string path, Document document, int from, int maxCharacters)
    {
        string text = Cut(document.Text, from, maxCharacters);
        int end = from + text.Length;
        var covered = document.Parts.Where(part => part.Start < end && part.Start + part.Length > from).ToList();

        return new JsonObject
        {
            ["path"] = path,
            ["kind"] = document.Format.ToString(),
            ["characters"] = new JsonObject { ["from"] = from, ["returned"] = text.Length, ["total"] = document.Text.Length },
            ["next"] = end < document.Text.Length ? end : null,
            ["parts"] = covered.Count == 0 ? null : new JsonArray([.. covered.Select(part => (JsonNode)new JsonObject
            {
                ["kind"] = part.Kind.ToString(),
                ["number"] = part.Number,
                ["title"] = part.Title
            })]),
            ["scans"] = covered.Any(part => part.IsScanned) ? new JsonArray([.. covered.Where(part => part.IsScanned).Select(part => (JsonNode)part.Number)]) : null,
            ["text"] = text
        };
    }

    /// <summary>The characters of <paramref name="text"/> from <paramref name="from"/>, never ending between the halves of one character.</summary>
    internal static string Cut(string text, int from, int maxCharacters)
    {
        int end = Math.Min(text.Length, from + maxCharacters);

        if (end > from && end < text.Length && char.IsHighSurrogate(text[end - 1]))
            end--;

        return text[from..end];
    }

    private static string? Refusal(int from, int? part, int maxCharacters)
    {
        if (from < 0)
            return Messages.OutOfRange("from", from, 0, int.MaxValue);

        if (part is < 1)
            return Messages.OutOfRange("part", part.Value, 1, int.MaxValue);

        return maxCharacters is < Limits.FewestCharacters or > Limits.MostCharacters
            ? Messages.OutOfRange("maxCharacters", maxCharacters, Limits.FewestCharacters, Limits.MostCharacters)
            : null;
    }
}
