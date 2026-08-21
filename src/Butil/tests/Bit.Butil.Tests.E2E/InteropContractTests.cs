using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

/// <summary>
/// Checks that every <c>BitButil.x.y</c> identifier the C# side invokes exists on the compiled
/// JavaScript bundle, and on the one lazy-loadable module file that identifier maps to when it is
/// evaluated on its own (what a lazy-scripts app does).
/// </summary>
/// <remarks>
/// A renamed or misspelled JS function compiles cleanly on both sides of the interop boundary and
/// only fails in the browser, on the single code path that calls it - which for a library of
/// mostly-optional browser APIs can easily be a path no test walks. Loading the real bundle and
/// resolving every call site catches the whole class at build time.
/// <br/>
/// Runs under Node, which is already a build dependency of Bit.Butil (it compiles the TypeScript),
/// and needs no browser - so unlike the rest of this suite it is fast and has nothing to install.
/// </remarks>
[TestClass]
public class InteropContractTests
{
    /// <summary>Set by MSTest; the run's log, which is where the script's report belongs.</summary>
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void Every_csharp_interop_call_resolves_against_the_bundle_and_its_lazy_module()
    {
        var root = LocateButilRoot();
        var script = Path.Combine(root, "tests", "Bit.Butil.Tests.E2E", "Infrastructure", "verify-interop-contract.mjs");
        var bundle = Path.Combine(root, "Bit.Butil", "wwwroot", "bit-butil.js");
        var sources = Path.Combine(root, "Bit.Butil");
        var modules = Path.Combine(root, "Bit.Butil", "wwwroot", "modules");

        Assert.IsTrue(File.Exists(script), $"The verification script is missing: {script}");
        Assert.IsTrue(File.Exists(bundle),
            $"The bundle is missing: {bundle}. Build Bit.Butil first - the bundle is generated, not checked in.");
        Assert.IsTrue(Directory.Exists(modules),
            $"The lazy-loadable modules are missing: {modules}. Build Bit.Butil first - they are generated alongside the bundle.");

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "node",
                ArgumentList = { script, bundle, sources, modules },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            },
        };

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            Assert.Inconclusive($"Node is not available on PATH, so the interop contract could not be checked: {ex.Message}");
            return;
        }

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Assert.AreEqual(0, process.ExitCode, $"{stderr}{stdout}");
        TestContext.WriteLine(stdout.Trim());
    }

    /// <summary>
    /// Walks up from the test binary to the <c>src/Butil</c> folder, so the test works from a
    /// build output directory whose depth differs between configurations and runners.
    /// </summary>
    private static string LocateButilRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Bit.Butil", "Bit.Butil.csproj")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not find the src/Butil folder walking up from {AppContext.BaseDirectory}.");
    }
}
