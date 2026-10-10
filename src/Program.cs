using Microsoft.Extensions.Hosting;
using Snail.MCP.Documents.Extensions;

await McpServerHost.Of(args).RunAsync();
