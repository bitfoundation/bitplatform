using System.Diagnostics;
using Meziantou.Framework.Win32;

namespace Bit.Websites.Platform.Server.Services.Mcp;

/// <summary>
/// Ties every documentation server this site starts to the site's own lifetime, through a job object the
/// kernel empties when the last handle to it closes. That covers the case no shutdown code can: the site
/// killed outright, by an app pool recycle or otherwise, which would leave its servers holding their ports.
/// Windows only; elsewhere they are stopped on shutdown like any other child.
/// </summary>
internal static class ChildProcessJob
{
    private static readonly Lock sync = new();

    // Held for the life of the process on purpose: closing this handle is what kills the servers.
    private static JobObject? job;

    private static bool unavailable;

    public static void Adopt(Process process, ILogger logger)
    {
        if (OperatingSystem.IsWindows() is false) return;

        lock (sync)
        {
            if (unavailable) return;

            try
            {
                if (job is null)
                {
                    job = new JobObject();
                    job.SetLimits(new() { Flags = JobObjectLimitFlags.KillOnJobClose });
                }

                job.AssignProcess(process);
            }
            catch (Exception exp)
            {
                // Only the guarantee is lost; the servers still run and are still stopped on a clean shutdown.
                unavailable = true;
                logger.LogWarning(exp, "The documentation servers could not be tied to this site's lifetime, so one that outlives a crash has to be stopped by hand.");
            }
        }
    }
}
