using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils;

/// <summary>
/// The scripts a package ships are compiled to the target of its tsconfig.json and then minified by esbuild in a Release
/// build, both by Bit.BlazorUI.Build/build-assets.mjs. esbuild left to its default target writes newer syntax (?? and ?.)
/// back into the script, so the package would ship a script the engines the tsconfig target is there for cannot parse,
/// which no Debug test would see.
/// </summary>
[TestClass]
public sealed partial class BitPackagedScriptTargetTests
{
    [TestMethod]
    [DataRow("Bit.BlazorUI")]
    [DataRow("Bit.BlazorUI.Extras")]
    [DataRow("Bit.BlazorUI.Legacy")]
    public void EveryMinifiedScriptShouldBeHeldToTheTargetTsconfigCompilesTo(string project)
    {
        var tsconfig = SourceFiles.Read(project, "tsconfig.json");
        Assert.IsTrue(TsconfigTarget().IsMatch(tsconfig), $"{project}/tsconfig.json names no compilerOptions.target.");

        // The package builds its scripts through the script, and minifies them there in a Release build - never through
        // an esbuild of its own, which would be left to its default target. The script and the switch come from the
        // targets every package imports, so no package sets them apart.
        var csproj = SourceFiles.Read(project, $"{project}.csproj");
        Assert.IsFalse(csproj.Contains("node_modules/.bin/esbuild"), $"{project}.csproj runs esbuild itself rather than through build-assets.mjs.");
        StringAssert.Contains(csproj, @"<Import Project=""..\Bit.BlazorUI.Build\Bit.BlazorUI.Assets.targets"" />");
        Assert.IsTrue(Regex.IsMatch(csproj, @"@\(BitAssetsScript->'%\(FullPath\)'\)&quot; scripts [^""]*\$\(_BitAssetsMinify\)"), $"{project}.csproj does not build its scripts through build-assets.mjs.");
        Assert.IsFalse(csproj.Contains("<_BitAssetsMinify"), $"{project}.csproj sets the minify switch of its own.");

        var shared = SourceFiles.Read("Bit.BlazorUI.Build", "Bit.BlazorUI.Assets.targets");
        StringAssert.Contains(shared, @"<BitAssetsScript Include=""$(MSBuildThisFileDirectory)build-assets.mjs"" />");
        StringAssert.Contains(shared, "<_BitAssetsMinify Condition=\" '$(Configuration)' == 'Release' \">--minify</_BitAssetsMinify>");

        // And the script minifies to the target the compiler was given, read off the tsconfig.
        var script = SourceFiles.Read("Bit.BlazorUI.Build", "build-assets.mjs");
        StringAssert.Contains(script, "const minifyTarget = esbuildTarget(ts, compilerOptions.target);");
        Assert.HasCount(1, EsbuildTransform().Matches(script), "build-assets.mjs was expected to minify in one place.");
        StringAssert.Contains(EsbuildTransform().Match(script).Value, "target: minifyTarget");
    }

    [GeneratedRegex("\"target\"\\s*:\\s*\"(?<target>[^\"]+)\"")]
    private static partial Regex TsconfigTarget();

    [GeneratedRegex(@"esbuild\.transformSync\([^)]*\)")]
    private static partial Regex EsbuildTransform();
}
