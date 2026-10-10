using System.Text;

namespace Snail.MCP.Documents.Adapters;

/// <summary>The text of a file whose encoding nobody wrote down.</summary>
/// <remarks>
/// A byte-order mark is believed. Without one the bytes are tried as UTF-8 strictly, and only when they are not
/// valid UTF-8 are they read as Windows-1251 — the encoding Excel and Notepad on a Russian Windows save a CSV in,
/// which as UTF-8 turns every Cyrillic letter into a replacement character and the table into noise.
/// </remarks>
internal static class Decoding
{
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    static Decoding() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public static string Text(Stream content)
    {
        using var buffer = new MemoryStream();
        content.CopyTo(buffer);
        byte[] bytes = buffer.ToArray();

        if (Marked(bytes) is { } marked)
            return marked;

        try
        {
            return StrictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.GetEncoding(1251).GetString(bytes);
        }
    }

    private static string? Marked(byte[] bytes)
    {
        if (bytes is [0xEF, 0xBB, 0xBF, ..])
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);

        if (bytes is [0xFF, 0xFE, ..])
            return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);

        return bytes is [0xFE, 0xFF, ..] ? Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2) : null;
    }
}
