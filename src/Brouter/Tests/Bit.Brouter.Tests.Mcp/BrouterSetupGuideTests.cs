using Bit.Brouter.Demo.Server.Services;

namespace Bit.Brouter.Tests.Mcp;

/// <summary>
/// The render-mode guides, at the level the tool cannot reach: the spellings a caller might arrive
/// with, and the fencing that decides whether a quoted file survives as code.
/// </summary>
[TestClass]
public class BrouterSetupGuideTests
{
    [TestMethod]
    public void A_render_mode_is_recognized_however_it_is_spelled()
    {
        // The tool's own argument is a closed set, but the same method answers the plain HTTP
        // endpoint, where a caller types what their project calls the render mode.
        (string Spelling, string Mode)[] spellings =
        [
            ("Server", "server"),
            ("  SERVER  ", "server"),
            ("InteractiveServer", "server"),
            ("blazor-server", "server"),
            ("WebAssembly", "wasm"),
            ("InteractiveWebAssembly", "wasm"),
            ("blazor wasm", "wasm"),
            ("InteractiveAuto", "auto"),
            ("standalone", "standalone-wasm"),
            ("standalone-webassembly", "standalone-wasm"),
        ];

        foreach (var (spelling, mode) in spellings)
        {
            Assert.AreEqual(BrouterSetupGuide.Get(mode), BrouterSetupGuide.Get(spelling), $"'{spelling}' did not resolve to '{mode}'.");
        }
    }

    [TestMethod]
    public void Anything_else_resolves_to_nothing_so_the_caller_can_be_told_what_does()
    {
        Assert.IsNull(BrouterSetupGuide.Get("maui"));
        Assert.IsNull(BrouterSetupGuide.Get(""));
        Assert.IsNull(BrouterSetupGuide.Get(null));
    }

    [TestMethod]
    public void Every_quoted_file_is_fenced_so_it_cannot_end_its_own_code_block()
    {
        // A file that itself shows Markdown would otherwise close the block halfway through, and the
        // rest of it would render as prose.
        foreach (var renderMode in BrouterSetupGuide.RenderModes)
        {
            var guide = BrouterSetupGuide.Get(renderMode)!;

            var fences = guide.Split('\n').Count(line => line.TrimStart().StartsWith("```", StringComparison.Ordinal));

            Assert.AreEqual(0, fences % 2, $"The '{renderMode}' guide has {fences} code fences, so one block is left open.");
        }
    }

    [TestMethod]
    public void A_quoted_file_is_labelled_with_the_language_it_is_written_in()
    {
        var guide = BrouterSetupGuide.Get("wasm")!;

        StringAssert.Contains(guide, "```csharp");
        StringAssert.Contains(guide, "```razor");
        StringAssert.Contains(guide, "```xml", "The sample's .csproj is quoted without a language, so nobody can tell what it is.");
    }

    [TestMethod]
    public void The_project_files_come_last_so_the_reader_meets_the_code_first()
    {
        var guide = BrouterSetupGuide.Get("wasm")!;

        var program = guide.IndexOf("Program.cs`", StringComparison.Ordinal);
        var csproj = guide.IndexOf(".csproj`", StringComparison.Ordinal);

        Assert.IsTrue(program > 0 && csproj > program, "The .csproj is quoted before the code it belongs to.");
    }

    [TestMethod]
    public void Every_hosted_mode_says_which_di_containers_have_to_register_the_services()
    {
        // Registering in one of a Web App's two containers is the single most common setup bug, and
        // it fails during prerendering rather than at compile time.
        StringAssert.Contains(BrouterSetupGuide.Get("wasm")!, "BOTH");
        StringAssert.Contains(BrouterSetupGuide.Get("auto")!, "both containers");
        StringAssert.Contains(BrouterSetupGuide.Get("server")!, "single (server) container");
        StringAssert.Contains(BrouterSetupGuide.Get("standalone-wasm")!, "One project, one DI container");
    }
}
