using Snail.Sdk.Mcp.Testing;

namespace Snail.MCP.Documents.Tests.Support;

/// <summary>The server at an address, as the platform starts it: the build beside the tests, or the program <c>SNAIL_DOCUMENTS_SERVER</c> names.</summary>
/// <remarks>
/// Given its port alone, through <c>SNAIL_MCP_DOCUMENTS_PORT</c> — a port is what makes this server listen rather than
/// speak stdio — on loopback, with a home of its own and no settings file, and handed back once <c>/healthz</c> answers.
/// </remarks>
internal static class Served
{
    public const string Assembly = "Snail.MCP.Documents";

    public const string Variable = "SNAIL_DOCUMENTS_SERVER";

    public static Task<HttpServerProgram> StartAsync(params (string Name, string Value)[] own) =>
        HttpServerProgram.StartAsync(
            Assembly,
            Variable,
            Deployment.Variables(own).ToDictionary(variable => variable.Key, variable => (string?)variable.Value),
            addressVariable: "SNAIL_MCP_DOCUMENTS_PORT");
}
