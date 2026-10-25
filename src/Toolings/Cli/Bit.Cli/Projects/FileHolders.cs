using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace Bit.Cli.Projects;

[SupportedOSPlatform("windows")]
public static class FileHolders
{
    private const int MaxFiles = 1000;
    private const int ErrorMoreData = 234;

    public static IReadOnlyList<int> Find(IEnumerable<string> files)
    {
        var paths = files.SelectMany(Expand).Distinct(StringComparer.OrdinalIgnoreCase).Take(MaxFiles).ToArray();

        if (paths.Length == 0 || RmStartSession(out var session, 0, new StringBuilder(33)) != 0)
            return [];

        try
        {
            if (RmRegisterResources(session, (uint)paths.Length, paths, 0, null, 0, null) != 0)
                return [];

            uint count = 0, reasons = 0;
            var result = RmGetList(session, out var needed, ref count, null, ref reasons);

            if (result != ErrorMoreData || needed == 0)
                return [];

            var infos = new RmProcessInfo[needed];
            count = needed;

            if (RmGetList(session, out needed, ref count, infos, ref reasons) != 0)
                return [];

            return [.. infos.Take((int)count).Select(i => i.Process.ProcessId).Distinct()];
        }
        finally
        {
            RmEndSession(session);
        }
    }

    private static IEnumerable<string> Expand(string path)
    {
        if (File.Exists(path))
            return [path];

        try
        {
            return Directory.Exists(path) ? [.. Directory.EnumerateFiles(path, "*", new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true }).Take(MaxFiles)] : [];
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RmUniqueProcess
    {
        public int ProcessId;
        public System.Runtime.InteropServices.ComTypes.FILETIME ProcessStartTime;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct RmProcessInfo
    {
        public RmUniqueProcess Process;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string AppName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string ServiceShortName;

        public int ApplicationType;
        public uint AppStatus;
        public uint TsSessionId;

        [MarshalAs(UnmanagedType.Bool)]
        public bool Restartable;
    }

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    private static extern int RmStartSession(out uint sessionHandle, int sessionFlags, StringBuilder sessionKey);

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    private static extern int RmRegisterResources(uint sessionHandle, uint fileCount, string[] fileNames, uint applicationCount, RmUniqueProcess[]? applications, uint serviceCount, string[]? serviceNames);

    [DllImport("rstrtmgr.dll")]
    private static extern int RmGetList(uint sessionHandle, out uint processInfoNeeded, ref uint processInfoCount, [In, Out] RmProcessInfo[]? affectedApps, ref uint rebootReasons);

    [DllImport("rstrtmgr.dll")]
    private static extern int RmEndSession(uint sessionHandle);
}
