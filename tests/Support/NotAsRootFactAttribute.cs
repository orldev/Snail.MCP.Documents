namespace Snail.MCP.Documents.Tests.Support;

/// <summary>A test of what this process may not read, skipped — not failed — when the tests run as root.</summary>
/// <remarks>
/// Root opens a file of mode 000 and enters a folder of it, so there is nothing to refuse: the Linux runner of
/// BuildBridge runs its container as root, and three such tests failed there while they pass for every other user.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class NotAsRootFactAttribute : FactAttribute
{
    public NotAsRootFactAttribute()
    {
        if (!OperatingSystem.IsWindows() && Environment.IsPrivilegedProcess)
            Skip = "checks a file the process may not open, and root may open every file";
    }
}
