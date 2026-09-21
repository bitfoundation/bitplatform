using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Extensions.JsInterop;

/// <summary>
/// Runs the JavaScript tests of <c>BitBlazorUI.Extras.initScripts</c> / <c>initStylesheets</c> - the loader
/// that decides whether a script or a stylesheet an app asks for has to be injected, or is already on the
/// page and only has to be waited for.
///
/// <para>
/// That loader is the one part of this library whose behaviour lives entirely in the browser: what it does
/// depends on a tag still being in flight, on a load event having already fired, on what Resource Timing will
/// admit about a cross-origin fetch. None of that can be reached from C#, and a contract test over the
/// sources can only check the call, not the conduct. So the JavaScript is tested as JavaScript, by
/// <c>Scripts/extras-resource-loader.test.js</c>, and this class is what makes it part of the suite.
/// </para>
///
/// <para>
/// It needs <c>node</c>, which is not a new dependency: both library projects build their TypeScript and
/// their stylesheets with it (<c>npm install</c>, <c>tsc</c>, <c>sass</c>), so anywhere the library builds,
/// node is there. The tests run against the SHIPPED bundle rather than a separate compilation of the
/// TypeScript, so what they exercise is what an app actually loads.
/// </para>
/// </summary>
[TestClass]
public class ExtrasResourceLoaderTests
{
    private const string BundleRelativePath = "Bit.BlazorUI.Extras/wwwroot/scripts/bit.blazorui.extras.js";
    private const string TestScriptRelativePath = "Scripts/extras-resource-loader.test.js";

    [TestMethod]
    public async Task ExtrasResourceLoader_ShouldPassItsJavaScriptTests()
    {
        var blazorUiRoot = JsInteropSources.TryFindBlazorUiRoot();
        var callerDirectory = JsInteropSources.TryFindCallerDirectory();

        if (blazorUiRoot is null || callerDirectory is null)
        {
            Assert.Inconclusive(
                "Skipped: could not locate the BlazorUI source tree. The JavaScript tests and the bundle they " +
                "run against are both read from it.");
            return;
        }

        var testScript = Path.Combine(callerDirectory, TestScriptRelativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.IsTrue(File.Exists(testScript), $"The JavaScript tests are missing: {testScript}");

        var bundle = Path.Combine(blazorUiRoot, BundleRelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(bundle) is false)
        {
            Assert.Inconclusive(
                $"Skipped: the Extras bundle has not been built ({bundle}). Building Bit.BlazorUI.Extras " +
                "compiles the TypeScript into it.");
            return;
        }

        AssertBundleIsNotStale(blazorUiRoot, bundle);

        var (exitCode, output) = await RunNodeAsync(testScript, bundle);

        if (exitCode == -1)
        {
            Assert.Inconclusive(
                "Skipped: node could not be started. It is required to build this library's TypeScript and " +
                "stylesheets, so this normally means the tests are running somewhere the library is not built." +
                Environment.NewLine + output);
            return;
        }

        Assert.AreEqual(0, exitCode,
            "The Extras resource loader's JavaScript tests failed:" + Environment.NewLine + output);
    }

    private static void AssertBundleIsNotStale(string blazorUiRoot, string bundle)
    {
        // The tests run against the compiled bundle, so a bundle older than the TypeScript would quietly
        // test the previous version of the loader. Building Bit.BlazorUI.Extras regenerates it (the csproj's
        // BuildJavaScript target takes the .ts files as its inputs and the bundle as its output), and this
        // project references that one, so in practice this only fires when the bundle was built by hand.
        var scripts = Path.Combine(blazorUiRoot, "Bit.BlazorUI.Extras", "Scripts");
        if (Directory.Exists(scripts) is false) return;

        var newestSource = Directory.EnumerateFiles(scripts, "*.ts", SearchOption.AllDirectories)
            .Select(File.GetLastWriteTimeUtc)
            .DefaultIfEmpty(DateTime.MinValue)
            .Max();

        Assert.IsTrue(File.GetLastWriteTimeUtc(bundle) >= newestSource,
            $"The Extras bundle ({bundle}) is older than the TypeScript it is compiled from, so these tests " +
            "would run against the previous version of the loader. Build Bit.BlazorUI.Extras.");
    }

    private static async Task<(int ExitCode, string Output)> RunNodeAsync(string testScript, string bundle)
    {
        var startInfo = new ProcessStartInfo("node")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(testScript)!,
        };
        startInfo.ArgumentList.Add(testScript);
        startInfo.ArgumentList.Add(bundle);

        var output = new StringBuilder();

        try
        {
            using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

            process.OutputDataReceived += (_, e) => { if (e.Data is not null) lock (output) output.AppendLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data is not null) lock (output) output.AppendLine(e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            lock (output) return (process.ExitCode, output.ToString());
        }
        catch (Exception exception)
        {
            // node is not on the PATH, or could not be started at all.
            return (-1, exception.Message);
        }
    }
}
