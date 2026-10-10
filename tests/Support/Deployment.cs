using Microsoft.Extensions.Configuration;
using Snail.MCP.Documents.Configuration;

namespace Snail.MCP.Documents.Tests.Support;

/// <summary>The environment a test starts the server in, written the way a deployment writes it.</summary>
internal static class Deployment
{
    public static Dictionary<string, string> Variables(params (string Name, string Value)[] own) =>
        own.ToDictionary(variable => $"{EnvironmentSettings.Prefix}{variable.Name}", variable => variable.Value);

    public static IConfiguration Settings(params (string Name, string Value)[] own) =>
        ServerConfigExtensions.Settings(Variables(own));
}
