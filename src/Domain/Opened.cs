namespace Snail.MCP.Documents.Domain;

/// <summary>A document opened for a call, or the reason it could not be.</summary>
public sealed record Opened(Document? Document, string? Error, string? Hint, string? Type = null)
{
    /// <summary>The document as read.</summary>
    public static Opened Of(Document document) => new(document, null, null);

    /// <summary>The refusal, with what to do instead and, when it has one, its kind.</summary>
    public static Opened Refused(string error, string? hint = null, string? type = null) => new(null, error, hint, type);

    /// <summary>The refusal as a tool answer.</summary>
    public Answer<JsonObject> Failure => Answer.Failed<JsonObject>(Error ?? "the document could not be opened", Hint, Type);
}
