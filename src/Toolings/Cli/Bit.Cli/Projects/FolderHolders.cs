using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace Bit.Cli.Projects;

[SupportedOSPlatform("windows")]
public static class FolderHolders
{
    private const int ProcessQueryInformation = 0x0400;
    private const int ProcessVmRead = 0x0010;
    private const int ProcessQueryLimitedInformation = 0x1000;
    private const int ProcessParametersOffset = 0x20;
    private const int CurrentDirectoryOffset = 0x38;

    public static IReadOnlyList<int> Find(string folder)
    {
        if (Environment.Is64BitProcess is false)
            return [];

        var root = Path.TrimEndingDirectorySeparator(folder);
        var ids = new List<int>();

        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                if (process.Id != Environment.ProcessId
                    && CurrentDirectoryOf(process.Id) is { } current
                    && (string.Equals(Path.TrimEndingDirectorySeparator(current), root, StringComparison.OrdinalIgnoreCase) || ProjectProcesses.IsUnder(current, root)))
                {
                    ids.Add(process.Id);
                }
            }
        }

        return ids;
    }

    public static int? ParentOf(int id)
    {
        var handle = OpenProcess(ProcessQueryLimitedInformation, false, id);

        if (handle == 0)
            return null;

        try
        {
            var info = new ProcessBasicInformation();

            return NtQueryInformationProcess(handle, 0, ref info, Marshal.SizeOf<ProcessBasicInformation>(), out _) == 0 ? (int)info.InheritedFromUniqueProcessId : null;
        }
        finally
        {
            CloseHandle(handle);
        }
    }

    private static string? CurrentDirectoryOf(int id)
    {
        var handle = OpenProcess(ProcessQueryInformation | ProcessVmRead, false, id);

        if (handle == 0)
            return null;

        try
        {
            var info = new ProcessBasicInformation();

            if (NtQueryInformationProcess(handle, 0, ref info, Marshal.SizeOf<ProcessBasicInformation>(), out _) != 0 || info.PebBaseAddress == 0)
                return null;

            var parameters = new byte[8];

            if (ReadProcessMemory(handle, info.PebBaseAddress + ProcessParametersOffset, parameters, parameters.Length, out _) is false)
                return null;

            var currentDirectory = new byte[16];

            if (ReadProcessMemory(handle, (nint)BitConverter.ToInt64(parameters) + CurrentDirectoryOffset, currentDirectory, currentDirectory.Length, out _) is false)
                return null;

            var length = BitConverter.ToUInt16(currentDirectory, 0);
            var buffer = (nint)BitConverter.ToInt64(currentDirectory, 8);

            if (length == 0 || buffer == 0)
                return null;

            var path = new byte[length];

            return ReadProcessMemory(handle, buffer, path, path.Length, out _) ? Encoding.Unicode.GetString(path) : null;
        }
        finally
        {
            CloseHandle(handle);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessBasicInformation
    {
        public nint ExitStatus;
        public nint PebBaseAddress;
        public nint AffinityMask;
        public nint BasePriority;
        public nint UniqueProcessId;
        public nint InheritedFromUniqueProcessId;
    }

    [DllImport("kernel32.dll")]
    private static extern nint OpenProcess(int desiredAccess, bool inheritHandle, int processId);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(nint handle);

    [DllImport("kernel32.dll")]
    private static extern bool ReadProcessMemory(nint process, nint baseAddress, byte[] buffer, int size, out nint bytesRead);

    [DllImport("ntdll.dll")]
    private static extern int NtQueryInformationProcess(nint process, int informationClass, ref ProcessBasicInformation information, int informationLength, out int returnLength);
}
