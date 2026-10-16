using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Snail.MCP.Documents.Adapters;

/// <summary>Opens a regular file at a path already resolved, refusing a link anywhere on it and never waiting on a pipe.</summary>
/// <remarks>
/// The path was resolved and checked a moment before; opened by name the ordinary way, a link swapped in between is
/// followed, and 988 reads went through one in five seconds. Opened here, a link anywhere on the path fails the open:
/// <c>O_NOFOLLOW_ANY</c> on macOS, <c>openat2</c> with <c>RESOLVE_NO_SYMLINKS</c> on Linux, and where the kernel has no
/// <c>openat2</c>, <c>O_NOFOLLOW</c>, which guards the last component only. <c>O_NONBLOCK</c> keeps a named pipe from
/// parking the thread in <c>open</c>, where no cancellation reaches; a pipe, a device or anything else that cannot seek
/// is refused once open. Windows opens the ordinary way. The C library is looked up only where the system keeps it, never
/// beside the program, and declared with <c>DllImport</c> rather than <c>LibraryImport</c>, which would need unsafe code
/// for three calls whose arguments are already blittable.
/// </remarks>
internal static class RegularFile
{
    private const int MacNonBlock = 0x4;

    private const int MacNoFollowAny = 0x2000_0000;

    private const int MacCloseOnExec = 0x100_0000;

    private const int MacLoop = 62;

    private const int LinuxNonBlock = 0x800;

    private const int LinuxCloseOnExec = 0x8_0000;

    private const int LinuxLoop = 40;

    private const int LinuxNoSystemCall = 38;

    private const int LinuxNotPermitted = 1;

    private const long OpenAt2 = 437;

    private const int CurrentDirectory = -100;

    private const ulong NoSymbolicLinks = 0x4;

    /// <summary>The file, open for reading.</summary>
    /// <exception cref="IOException">A link is on the path, the file is not a regular one, or it could not be opened.</exception>
    public static FileStream Opened(string path)
    {
        if (OperatingSystem.IsWindows())
            return File.OpenRead(path);

        int descriptor = OperatingSystem.IsMacOS() ? OnMac(path) : OnLinux(path);

        if (descriptor < 0)
            throw Failure(path, Marshal.GetLastPInvokeError());

        var file = Wrapped(new SafeFileHandle(descriptor, ownsHandle: true));

        if (file.CanSeek)
            return file;

        file.Dispose();

        throw new IOException($"'{path}' is not a regular file");
    }

    /// <summary>The descriptor as a stream, closed rather than leaked when the stream cannot be made of it.</summary>
    private static FileStream Wrapped(SafeFileHandle handle)
    {
        try
        {
            return new FileStream(handle, FileAccess.Read, bufferSize: 1);
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    private static int OnMac(string path) =>
        MacOpen(Terminated(path), MacNonBlock | MacNoFollowAny | MacCloseOnExec);

    private static int OnLinux(string path)
    {
        byte[] name = Terminated(path);
        var how = new OpenHow(LinuxNonBlock | LinuxCloseOnExec, 0, NoSymbolicLinks);

        long descriptor = LinuxSystemCall(OpenAt2, CurrentDirectory, name, ref how, (nuint)Marshal.SizeOf<OpenHow>());

        if (descriptor >= 0 || Marshal.GetLastPInvokeError() is not (LinuxNoSystemCall or LinuxNotPermitted))
            return (int)descriptor;

        return LinuxOpen(name, LinuxNonBlock | LinuxCloseOnExec | LinuxNoFollow());
    }

    /// <summary><c>O_NOFOLLOW</c>, whose value differs between the architectures Linux runs this server on.</summary>
    private static int LinuxNoFollow() =>
        RuntimeInformation.ProcessArchitecture is Architecture.Arm64 or Architecture.Arm ? 0x8000 : 0x2_0000;

    private static IOException Failure(string path, int error)
    {
        bool isThroughALink = OperatingSystem.IsMacOS() ? error == MacLoop : error == LinuxLoop;

        return isThroughALink
            ? new IOException($"'{path}' leads through a link that appeared after it was checked")
            : new IOException($"'{path}': {Marshal.GetPInvokeErrorMessage(error)}");
    }

    private static byte[] Terminated(string path) => Encoding.UTF8.GetBytes($"{path}\0");

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct OpenHow(ulong Flags, ulong Mode, ulong Resolve);

    [DllImport("libc", EntryPoint = "open", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int MacOpen(byte[] path, int flags);

    [DllImport("libc.so.6", EntryPoint = "open", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int LinuxOpen(byte[] path, int flags);

    [DllImport("libc.so.6", EntryPoint = "syscall", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern long LinuxSystemCall(long number, int directory, byte[] path, ref OpenHow how, nuint size);
}
