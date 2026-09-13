namespace Bit.Bmotion.Tests.Mcp.Services;

/// <summary>
/// The per-render-mode setup guide. Getting this wrong is the setup bug that does not fail at
/// compile time: registering the services in only one of a Blazor Web App's two DI containers
/// throws during prerendering, in production, on a page that worked locally. So each guide has to
/// carry the wiring for every container its render mode actually uses, and has to be honest about
/// which library features that mode cannot run at all.
/// </summary>
[TestClass]
public class SetupGuideTests
{
    [TestMethod]
    public void Get_AnswersForEveryAdvertisedRenderMode()
    {
        foreach (var mode in BmotionSetupGuide.RenderModes)
        {
            var guide = BmotionSetupGuide.Get(mode);

            Assert.IsNotNull(guide, $"'{mode}' is advertised but has no guide.");
            Assert.IsTrue(guide.Length > 500, $"The '{mode}' guide is only {guide.Length} characters.");
        }
    }

    [TestMethod]
    public void Get_EveryGuide_CarriesTheStepsThatCannotBeSkipped()
    {
        foreach (var mode in BmotionSetupGuide.RenderModes)
        {
            var guide = BmotionSetupGuide.Get(mode)!;

            StringAssert.Contains(guide, "dotnet add package Bit.Bmotion", $"The '{mode}' guide does not say how to install it.");
            StringAssert.Contains(guide, "_Imports.razor", $"The '{mode}' guide omits the @using nobody remembers.");
            StringAssert.Contains(guide, "<Bmotion", $"The '{mode}' guide shows no animation to prove the wiring works.");

            // Registration is either written out or factored into the shared extension method the
            // guide points at; what matters is that following this one guide reaches it.
            Assert.IsTrue(guide.Contains("AddBitBmotionServices", StringComparison.Ordinal) ||
                          guide.Contains("AddDemoServices", StringComparison.Ordinal),
                          $"The '{mode}' guide never registers the services.");
        }
    }

    /// <summary>
    /// The whole point of splitting the guide by render mode: each one has to say what it costs.
    /// A guide that reads identically for Server and WebAssembly would be worse than none, because
    /// it is exactly the difference that is invisible until production.
    /// </summary>
    [TestMethod]
    public void Get_TheServerGuide_SaysWhatDoesNotWorkThere()
    {
        var server = BmotionSetupGuide.Get("server")!;

        StringAssert.Contains(server, "Blazor Server");
        Assert.IsTrue(server.Contains("drag", StringComparison.OrdinalIgnoreCase),
                      "The Server guide does not mention that drag is unavailable.");
        Assert.IsTrue(server.Contains("compositor", StringComparison.OrdinalIgnoreCase) ||
                      server.Contains("frame loop", StringComparison.OrdinalIgnoreCase),
                      "The Server guide does not explain which animations survive.");
    }

    [TestMethod]
    public void Get_TheGuidesForDifferentModes_AreActuallyDifferent()
    {
        var guides = BmotionSetupGuide.RenderModes.ToDictionary(mode => mode, mode => BmotionSetupGuide.Get(mode)!);

        foreach (var (mode, guide) in guides)
        {
            foreach (var (other, otherGuide) in guides.Where(entry => entry.Key != mode))
            {
                Assert.AreNotEqual(guide, otherGuide, $"The '{mode}' and '{other}' guides are the same text.");
            }
        }
    }

    /// <summary>
    /// A Blazor Web App has two DI containers and the services must be registered in both. The
    /// wasm and auto guides are the ones where forgetting the second is the classic failure.
    /// </summary>
    [TestMethod]
    [DataRow("wasm")]
    [DataRow("auto")]
    public void Get_TheWebAppGuides_RegisterInBothContainers(string mode)
    {
        var guide = BmotionSetupGuide.Get(mode)!;

        StringAssert.Contains(guide, "Server/Program.cs", $"The '{mode}' guide does not wire the server container.");
        StringAssert.Contains(guide, "Client/Program.cs", $"The '{mode}' guide does not wire the client container.");

        Assert.IsTrue(guide.Contains("BOTH", StringComparison.OrdinalIgnoreCase) ||
                      guide.Contains("both containers", StringComparison.OrdinalIgnoreCase),
                      $"The '{mode}' guide does not say out loud that both containers are required - " +
                      "which is the step that fails during prerendering rather than at compile time.");
    }

    [TestMethod]
    [DataRow("webassembly", "wasm")]
    [DataRow("InteractiveWebAssembly", "wasm")]
    [DataRow("Blazor Server", "server")]
    [DataRow("interactiveserver", "server")]
    [DataRow("InteractiveAuto", "auto")]
    [DataRow("standalone", "standalone-wasm")]
    [DataRow("  SERVER  ", "server")]
    public void Get_AcceptsTheNamesARenderModeIsActuallyCalledBy(string alias, string canonical)
    {
        Assert.AreEqual(BmotionSetupGuide.Get(canonical), BmotionSetupGuide.Get(alias),
                        $"'{alias}' did not resolve to the '{canonical}' guide.");
    }

    /// <summary>
    /// An unrecognised mode must not quietly return the WebAssembly guide: that is how an agent
    /// ends up wiring a Server app as if it were WebAssembly, which compiles and then does nothing.
    /// </summary>
    [TestMethod]
    [DataRow("blazor")]
    [DataRow("maui")]
    [DataRow("ssr")]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow(null)]
    public void Get_AnUnknownMode_IsNullRatherThanADefaultGuess(string? mode)
    {
        Assert.IsNull(BmotionSetupGuide.Get(mode!));
    }
}
