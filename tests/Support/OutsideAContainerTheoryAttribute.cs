namespace Snail.MCP.Documents.Tests.Support;

/// <summary>A test of what the server refuses outside a container, skipped — not failed — inside one.</summary>
/// <remarks>
/// Inside a container the same setting is allowed, so there is nothing to refuse, and Snail.Sdk reads the variable from
/// the process itself, so a test cannot pretend either way: the Linux runner of BuildBridge is a container, and the
/// theory of an address beyond loopback failed there while it passes on a machine.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class OutsideAContainerTheoryAttribute : TheoryAttribute
{
    public const string Variable = "DOTNET_RUNNING_IN_CONTAINER";

    public OutsideAContainerTheoryAttribute()
    {
        if (string.Equals(Environment.GetEnvironmentVariable(Variable), "true", StringComparison.OrdinalIgnoreCase))
            Skip = $"checks a refusal outside a container, and {Variable}=true says this is one";
    }
}
