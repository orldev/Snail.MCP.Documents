namespace Snail.MCP.Documents.Tests.Support;

/// <summary>The checkout the tests run from, found by its solution file rather than by today's layout.</summary>
internal static class Repository
{
    public static string Root { get; } = Found();

    public static string Read(params string[] path) => File.ReadAllText(Path.Combine([Root, .. path]));

    private static string Found()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            if (folder.EnumerateFiles("*.slnx").Any())
                return folder.FullName;
        }

        throw new InvalidOperationException($"no solution file above {AppContext.BaseDirectory}");
    }
}
