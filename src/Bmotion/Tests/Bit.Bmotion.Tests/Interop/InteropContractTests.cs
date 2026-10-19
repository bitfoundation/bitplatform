using System.Text.RegularExpressions;
using Bit.Bmotion.Tests.TestInfra;

namespace Bit.Bmotion.Tests.Interop;

/// <summary>
/// The C#↔JS contract, checked from the sources: every function <see cref="BmotionInterop"/>
/// invokes by name must be exported by <c>bit-bmotion.ts</c>, and every call the test fake records
/// must name one of those functions. A rename on either side otherwise compiles, passes every
/// fake-backed test, and fails only in a browser.
/// </summary>
[TestClass]
public partial class InteropContractTests
{
    // InvokeAsync<T>("name", …) / InvokeVoidAsync("name", …), possibly with the name on the next line.
    [GeneratedRegex(@"Invoke(?:Void)?Async(?:<[^>()]*>)?\(\s*""(?<name>[^""]+)""")]
    private static partial Regex InvokedFunction();

    [GeneratedRegex(@"^export\s+(?:async\s+)?function\s+(?<name>\w+)", RegexOptions.Multiline)]
    private static partial Regex ExportedFunction();

    [GeneratedRegex(@"Record\(""(?<name>[^""]+)""")]
    private static partial Regex RecordedCall();

    // The Bmotion folder (holding Bit.Bmotion/ and Tests/), found by walking up from the test binaries
    // so the test works from any output directory and target framework.
    private static string BmotionRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Bit.Bmotion", "Interop", "BmotionInterop.cs")))
                return dir.FullName;
        }
        throw new InvalidOperationException($"Could not find Bit.Bmotion/Interop/BmotionInterop.cs above {AppContext.BaseDirectory}.");
    }

    private static string Read(params string[] relative) => File.ReadAllText(Path.Combine([BmotionRoot(), .. relative]));

    private static HashSet<string> Matches(Regex regex, string text)
        => regex.Matches(text).Select(m => m.Groups["name"].Value).ToHashSet(StringComparer.Ordinal);

    private static HashSet<string> InvokedByTheBridge()
    {
        var invoked = Matches(InvokedFunction(), Read("Bit.Bmotion", "Interop", "BmotionInterop.cs"));
        invoked.Remove("import"); // the module import itself, not a function of the module
        return invoked;
    }

    private static HashSet<string> ExportedByTheModule() => Matches(ExportedFunction(), Read("Bit.Bmotion", "Scripts", "bit-bmotion.ts"));

    [TestMethod]
    public void TheExtraction_FindsTheBridgesCalls()
    {
        // Guards the regexes: a pattern that silently matched nothing would make the contract vacuous.
        var invoked = InvokedByTheBridge();
        Assert.IsTrue(invoked.Count >= 20, $"only found {invoked.Count} invoked functions: {string.Join(", ", invoked)}");
        CollectionAssert.IsSubsetOf(new[] { "startRafLoop", "getBoundingRect", "playWaapiFlip", "playScrollTimelineAnimation" }, invoked.ToArray());
        Assert.IsTrue(ExportedByTheModule().Contains("startViewTransition"), "async exports must be recognised too");
    }

    [TestMethod]
    public void EveryFunctionTheBridgeInvokes_IsExportedByTheModule()
    {
        var missing = InvokedByTheBridge().Except(ExportedByTheModule()).Order().ToArray();

        Assert.AreEqual(0, missing.Length, $"BmotionInterop invokes functions bit-bmotion.ts does not export: {string.Join(", ", missing)}");
    }

    [TestMethod]
    public void EveryCallTheFakeRecords_NamesARealBridgeFunction()
    {
        var recorded = Matches(RecordedCall(), Read("Tests", "Bit.Bmotion.Tests", "TestInfra", "FakeBmotionInterop.cs"));
        recorded.Remove("dispose"); // IAsyncDisposable, not a JS call

        var unknown = recorded.Except(InvokedByTheBridge()).Order().ToArray();

        Assert.AreEqual(0, unknown.Length,
            $"the fake records calls the real bridge never makes, so assertions on them prove nothing: {string.Join(", ", unknown)}");
    }
}
