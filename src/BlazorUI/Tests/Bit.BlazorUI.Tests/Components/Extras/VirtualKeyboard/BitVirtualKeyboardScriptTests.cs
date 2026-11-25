using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.VirtualKeyboard;

/// <summary>
/// Runs the node:test suite of the BitVirtualKeyboard text engine - the Scripts/*.test.mjs files beside this one - against the
/// module Bit.BlazorUI.Extras compiles it into (wwwroot/scripts/modules/BitVirtualKeyboardEngine.js), and fails with node's
/// own report when any of it fails. The engine's logic (Backspace, insert with limits, dead keys, multitap, Shift) is plain
/// JavaScript a bUnit render never runs. The test is inconclusive only where no Node.js can be started at all.
/// </summary>
[TestClass]
public sealed class BitVirtualKeyboardScriptTests
{
    private static readonly TimeSpan RunTimeout = TimeSpan.FromMinutes(3);

    [TestMethod]
    public void TheEngineScriptSuiteShouldPassAgainstTheCompiledModules()
    {
        var scripts = SourceFiles.GetDirectory("Tests", "Bit.BlazorUI.Tests", "Components", "Extras", "VirtualKeyboard", "Scripts");
        var modules = SourceFiles.GetPath("Bit.BlazorUI.Extras", "wwwroot", "scripts", "modules");
        var engine = Path.Combine(modules, "BitVirtualKeyboardEngine.js");

        Assert.IsTrue(File.Exists(engine), $"Missing {engine}; building Bit.BlazorUI.Extras compiles the engine's TypeScript into it.");

        var files = Directory.GetFiles(scripts, "*.test.mjs").OrderBy(f => f, StringComparer.Ordinal).ToArray();

        Assert.IsTrue(files.Length > 0, $"No *.test.mjs file in {scripts}.");

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
        info.Environment["BIT_VKB_MODULES"] = modules;
        info.Environment["NO_COLOR"] = "1";
        info.Environment.Remove("FORCE_COLOR");

        var output = new StringBuilder();
        using var process = new Process { StartInfo = info };
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) lock (output) output.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) lock (output) output.AppendLine(e.Data); };

        try
        {
            process.Start();
        }
        catch (Win32Exception e)
        {
            Assert.Inconclusive($"node could not be started ({e.Message}); the engine's script tests need Node.js on the PATH.");
            return;
        }

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
