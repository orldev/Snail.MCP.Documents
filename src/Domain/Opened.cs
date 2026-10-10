namespace Snail.MCP.Documents.Domain;

/// <summary>A document opened for a call, or the reason it could not be.</summary>
public sealed record Opened(Document? Document, string? Error, string? Hint)
{
    /// <summary>The document as read.</summary>
    public static Opened Of(Document document) => new(document, null, null);

    /// <summary>The refusal, with what to do instead.</summary>
    public static Opened Refused(string error, string? hint = null) => new(null, error, hint);

    /// <summary>The refusal as a tool answer.</summary>
    public Answer Failure => Answer.Failed(Error ?? "the document could not be opened", Hint);
}
