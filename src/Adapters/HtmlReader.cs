using System.Text;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace Snail.MCP.Documents.Adapters;

/// <summary>A saved web page: its headings, paragraphs, lists, tables and code, without menus, scripts and styles.</summary>
/// <remarks>
/// Text that sits loose inside a container — a <c>div</c> with words and links but no paragraph — is gathered
/// into a paragraph of its own, because most pages are written that way and a reader that waited for
/// <c>p</c> would find them empty.
/// </remarks>
public sealed class HtmlReader : IFormatReader
{
    private static readonly HashSet<string> Skipped = new(StringComparer.OrdinalIgnoreCase) { "script", "style", "noscript", "template", "svg", "nav", "iframe", "head", "form", "button" };

    private static readonly HashSet<string> Inline = new(StringComparer.OrdinalIgnoreCase) { "a", "span", "strong", "b", "em", "i", "u", "code", "small", "sub", "sup", "mark", "abbr", "time", "label", "br", "img", "s", "q", "cite", "kbd", "var" };

    /// <inheritdoc/>
    public DocumentKind Format => DocumentKind.Html;

    /// <inheritdoc/>
    public IReadOnlyList<string> Extensions { get; } = [".html", ".htm"];

    /// <inheritdoc/>
    public IReadOnlyList<Block> Read(Stream content)
    {
        var page = new HtmlParser().ParseDocument(Decoding.Text(content));
        List<Block> blocks = [];

        if (page.Title is { Length: > 0 } title && page.QuerySelector("h1") is null)
            blocks.Add(new Heading(1, title.Trim()));

        var loose = new StringBuilder();
        Walk(page.Body ?? page.DocumentElement, blocks, loose, depth: 0);
        Flush(blocks, loose);

        return blocks;
    }

    private static void Walk(INode node, List<Block> blocks, StringBuilder loose, int depth)
    {
        foreach (var child in node.ChildNodes)
        {
            if (child is IText text)
            {
                loose.Append(text.Data);
                continue;
            }

            if (child is not IElement element || Skipped.Contains(element.LocalName))
                continue;

            if (Inline.Contains(element.LocalName))
            {
                loose.Append(element.LocalName.Equals("br", StringComparison.OrdinalIgnoreCase) ? "\n" : element.TextContent);
                continue;
            }

            Flush(blocks, loose);
            Read(element, blocks, depth);
        }
    }

    private static void Read(IElement element, List<Block> blocks, int depth)
    {
        switch (element.LocalName.ToLowerInvariant())
        {
            case "h1" or "h2" or "h3" or "h4" or "h5" or "h6":
                blocks.Add(new Heading(element.LocalName[1] - '0', Collapsed(element.TextContent)));
                break;
            case "p" or "blockquote" or "dt" or "dd" or "figcaption" or "caption":
                blocks.Add(new Paragraph(Collapsed(element.TextContent)));
                break;
            case "pre":
                blocks.Add(new CodeBlock(string.Empty, element.TextContent));
                break;
            case "table":
                blocks.Add(new TableBlock(element.QuerySelector("caption")?.TextContent.Trim(), Rows(element)));
                break;
            case "ul" or "ol":
                Items(element, blocks, depth);
                break;
            default:
                var loose = new StringBuilder();
                Walk(element, blocks, loose, depth);
                Flush(blocks, loose);
                break;
        }
    }

    private static void Items(IElement list, List<Block> blocks, int depth)
    {
        bool isOrdered = list.LocalName.Equals("ol", StringComparison.OrdinalIgnoreCase);

        foreach (var item in list.Children.Where(child => child.LocalName.Equals("li", StringComparison.OrdinalIgnoreCase)))
        {
            string own = Collapsed(string.Concat(item.ChildNodes.Where(node => node is not IElement nested || (nested.LocalName is not ("ul" or "ol"))).Select(node => node.TextContent)));
            blocks.Add(new ListItem(depth, own, isOrdered));

            foreach (var nested in item.Children.Where(child => child.LocalName is "ul" or "ol"))
                Items(nested, blocks, depth + 1);
        }
    }

    private static IReadOnlyList<IReadOnlyList<string>> Rows(IElement table) =>
    [
        .. table.QuerySelectorAll("tr")
            .Where(row => row.Closest("table") == table)
            .Select(row => (IReadOnlyList<string>)[.. row.Children.Where(cell => cell.LocalName is "td" or "th").Select(cell => Collapsed(cell.TextContent))])
            .Where(row => row.Count > 0)
    ];

    private static void Flush(List<Block> blocks, StringBuilder loose)
    {
        if (Collapsed(loose.ToString()) is { Length: > 0 } text)
            blocks.Add(new Paragraph(text));

        loose.Clear();
    }

    private static string Collapsed(string text) =>
        string.Join('\n', text.Split('\n').Select(line => string.Join(' ', line.Split((char[])[' ', '\t', '\r', ' '], StringSplitOptions.RemoveEmptyEntries))).Where(line => line.Length > 0));
}
