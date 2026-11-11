using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.ResourceTimeline;

/// <summary>
/// Runs the node:test suite of the BitResourceTimeline rendering engine - the Scripts/*.test.mjs files beside this one -
/// against the modules Bit.BlazorUI.Extras compiles its TypeScript into (wwwroot/scripts/modules/BitResourceTimeline*.js),
/// and fails with node's own report when any of it fails.
/// </summary>
/// <remarks>
/// The engine's zone math, allocation index, lane stacking, scene building, hit-testing, scrolling, editing, selection,
/// texts, theming and tooltip are plain JavaScript that a bUnit render never runs, so they are tested in Node instead,
/// on a bare engine built on the class's prototype (see Scripts/helpers/engine-fixture.mjs and Scripts/README.md). The
/// modules are handed to that fixture through the BIT_RTM_MODULES environment variable, read from the source checkout
/// through <see cref="SourceFiles"/> rather than from a copy beside the binaries. The test is inconclusive only where no
/// Node.js can be started at all.
/// </remarks>
[TestClass]
public sealed class BitResourceTimelineScriptTests
{
    private static readonly TimeSpan RunTimeout = TimeSpan.FromMinutes(3);

    [TestMethod]
    public void TheEngineScriptSuiteShouldPassAgainstTheCompiledModules()
    {
        var scripts = SourceFiles.GetDirectory("Tests", "Bit.BlazorUI.Tests", "Components", "Extras", "ResourceTimeline", "Scripts");
        var modules = SourceFiles.GetPath("Bit.BlazorUI.Extras", "wwwroot", "scripts", "modules");
        var engine = Path.Combine(modules, "BitResourceTimelineEngine.js");

        Assert.IsTrue(File.Exists(engine), $"Missing {engine}; building Bit.BlazorUI.Extras compiles the engine's TypeScript into it.");

        var files = Directory.GetFiles(scripts, "*.test.mjs").OrderBy(f => f, StringComparer.Ordinal).ToArray();

        Assert.IsNotEmpty(files, $"No *.test.mjs file in {scripts}.");

        var info = new ProcessStartInfo("node")
        {
            WorkingDirectory = scripts,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        info.ArgumentList.Add("--test");
        // The spec reporter whether or not the output is a terminal, so a failure reads the same everywhere.
        info.ArgumentList.Add("--test-reporter=spec");
        foreach (var file in files) info.ArgumentList.Add(file);
        info.Environment["BIT_RTM_MODULES"] = modules;
        info.Environment["NO_COLOR"] = "1";
        info.Environment.Remove("FORCE_COLOR");

        var output = new StringBuilder();
        using var process = new Process { StartInfo = info };
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) lock (output) output.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) lock (output) output.AppendLine(e.Data); };

        string? startFailure = null;
        try
        {
            process.Start();
        }
        catch (Win32Exception e)
        {
            startFailure = e.Message;
        }

        if (startFailure is not null)
            throw new AssertInconclusiveException($"node could not be started ({startFailure}); the engine's script tests need Node.js on the PATH.");

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (process.WaitForExit(RunTimeout) is false)
        {
            try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
            process.WaitForExit();
            Assert.Fail($"node --test did not finish in {RunTimeout}.{Environment.NewLine}{Report(output)}");
        }

        // The timed wait returns before the redirected streams are drained; this one waits for them too.
        process.WaitForExit();

        Assert.AreEqual(0, process.ExitCode, $"node --test exited with {process.ExitCode}.{Environment.NewLine}{Report(output)}");
    }

    private static string Report(StringBuilder output)
    {
        lock (output) return output.ToString();
    }
}
