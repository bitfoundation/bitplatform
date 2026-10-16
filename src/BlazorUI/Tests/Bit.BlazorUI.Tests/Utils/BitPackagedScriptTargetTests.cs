using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils;

/// <summary>
/// The scripts a package ships are compiled by tsc to the target of its tsconfig.json and then minified by esbuild in a
/// Release build. esbuild left to its default target writes newer syntax (?? and ?.) back into the script, so the
/// package would ship a script the engines the tsconfig target is there for cannot parse, which no Debug test would see.
/// </summary>
[TestClass]
public sealed partial class BitPackagedScriptTargetTests
{
    [TestMethod]
    [DataRow("Bit.BlazorUI")]
    [DataRow("Bit.BlazorUI.Extras")]
    public void EveryMinifiedScriptShouldBeHeldToTheTargetTsconfigCompilesTo(string project)
    {
        var tsconfig = SourceFiles.Read(project, "tsconfig.json");
        var target = TsconfigTarget().Match(tsconfig);
        Assert.IsTrue(target.Success, $"{project}/tsconfig.json names no compilerOptions.target.");

        var csproj = SourceFiles.Read(project, $"{project}.csproj");
        var esbuildCommands = EsbuildCommand().Matches(csproj).Select(m => m.Value).ToArray();
        Assert.IsNotEmpty(esbuildCommands, $"{project}.csproj runs no esbuild; update this test if the minifier changed.");

        var expected = $"--target={target.Groups["target"].Value.ToLowerInvariant()}";
        foreach (var command in esbuildCommands)
        {
            Assert.Contains(expected, command, $"{project}.csproj minifies without {expected}, so the shipped script may hold syntax newer than {target.Groups["target"].Value}: {command}");
        }
    }

    [GeneratedRegex("\"target\"\\s*:\\s*\"(?<target>[^\"]+)\"")]
    private static partial Regex TsconfigTarget();

    [GeneratedRegex("node_modules/\\.bin/esbuild[^\"]*")]
    private static partial Regex EsbuildCommand();
}
