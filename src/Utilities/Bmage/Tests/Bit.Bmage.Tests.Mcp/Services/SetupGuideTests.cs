namespace Bit.Bmage.Tests.Mcp.Services;

/// <summary>
/// The setup guides state the facts that differ per hosting model - which registration, which
/// containers, which codecs - and those are the facts asserted here, one model at a time.
/// </summary>
[TestClass]
public class SetupGuideTests
{
    [TestMethod]
    public void EveryMode_HasAGuide_WithATitleAndCode()
    {
        foreach (var mode in BmageSetupGuide.RenderModes)
        {
            var guide = BmageSetupGuide.Get(mode);

            Assert.IsNotNull(guide, mode);
            StringAssert.StartsWith(guide, "# Bit.Bmage");
            StringAssert.Contains(guide, "```csharp");
        }
    }

    [TestMethod]
    public void TheBrowserModes_UseTheBrowserRegistration_AndWarnAboutPrerendering()
    {
        foreach (var mode in new[] { "wasm", "server", "auto", "standalone-wasm", "hybrid" })
        {
            var guide = BmageSetupGuide.Get(mode)!;

            StringAssert.Contains(guide, "AddBitBmageServices", mode);
            StringAssert.Contains(guide, "bit-bmage-editor.css", mode);
        }

        foreach (var mode in new[] { "wasm", "server", "auto" })
        {
            StringAssert.Contains(BmageSetupGuide.Get(mode), "OnAfterRenderAsync", mode);
        }
    }

    [TestMethod]
    public void TheWebAppModes_RegisterInBothContainers()
    {
        foreach (var mode in new[] { "wasm", "auto" })
        {
            var guide = BmageSetupGuide.Get(mode)!;

            StringAssert.Contains(guide, "BOTH containers", mode);
            StringAssert.Contains(guide, "Client/Program.cs", mode);
            StringAssert.Contains(guide, "Server/Program.cs", mode);
        }
    }

    [TestMethod]
    public void TheWebAssemblyModes_EnableAot()
    {
        foreach (var mode in new[] { "wasm", "auto", "standalone-wasm" })
        {
            StringAssert.Contains(BmageSetupGuide.Get(mode), "<RunAOTCompilation>true</RunAOTCompilation>", mode);
        }

        StringAssert.Contains(BmageSetupGuide.Get("server"), "RunAOTCompilation` does not apply");
    }

    [TestMethod]
    public void TheHeadlessMode_UsesTheHeadlessRegistration_AndRulesOutTheBrowserCodecs()
    {
        var guide = BmageSetupGuide.Get("headless")!;

        StringAssert.Contains(guide, "AddBitBmageHeadlessServices");
        StringAssert.Contains(guide, "processor.Identify(bytes)");
        StringAssert.Contains(guide, "ImageCapabilityException");
        Assert.IsFalse(guide.Contains("bit-bmage-editor.css", StringComparison.Ordinal), "The headless guide tells a server to link a stylesheet.");
        Assert.IsFalse(guide.Contains("ImageFormat.Avif", StringComparison.Ordinal), "The headless guide encodes AVIF, which it cannot.");
        StringAssert.Contains(guide, "Lossless = true");
    }

    [TestMethod]
    public void TheHybridMode_RegistersOnce_AndSavesThroughThePlatform()
    {
        var guide = BmageSetupGuide.Get("hybrid")!;

        StringAssert.Contains(guide, "AddMauiBlazorWebView()");
        StringAssert.Contains(guide, "ImageEnvironment.BrowserServer");
        StringAssert.Contains(guide, "wwwroot/index.html");
        StringAssert.Contains(guide, "Share.Default.RequestAsync");
        StringAssert.Contains(guide, "UseInterpreter");
        Assert.IsFalse(guide.Contains("BOTH containers", StringComparison.Ordinal), "The hybrid guide asks for a second container a hybrid app does not have.");
        Assert.IsFalse(guide.Contains("<RunAOTCompilation>true", StringComparison.Ordinal), "The hybrid guide turns on WebAssembly AOT, which does not apply.");
    }

    [TestMethod]
    [DataRow("InteractiveWebAssembly", "wasm")]
    [DataRow("WebAssembly", "wasm")]
    [DataRow("interactiveserver", "server")]
    [DataRow("InteractiveAuto", "auto")]
    [DataRow("standalone", "standalone-wasm")]
    [DataRow("MAUI", "hybrid")]
    [DataRow("Blazor Hybrid", "hybrid")]
    [DataRow("wpf", "hybrid")]
    [DataRow("Windows Forms", "hybrid")]
    [DataRow("console", "headless")]
    [DataRow(" ASP.NET Core ", "headless")]
    [DataRow("worker", "headless")]
    public void Aliases_ResolveToTheirMode(string alias, string mode)
    {
        Assert.AreEqual(mode, BmageSetupGuide.Resolve(alias));
        Assert.AreEqual(BmageSetupGuide.Get(mode), BmageSetupGuide.Get(alias));
    }

    [TestMethod]
    public void AnUnknownMode_IsNull()
    {
        Assert.IsNull(BmageSetupGuide.Get("electron"));
        Assert.IsNull(BmageSetupGuide.Get(null));
    }

    /// <summary>Every source file a guide points at has to be one the server can hand out.</summary>
    [TestMethod]
    public void EverySourceFileAGuideNames_Exists()
    {
        foreach (var mode in BmageSetupGuide.RenderModes)
        {
            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(BmageSetupGuide.Get(mode)!, "path: \"(?<path>[^\"]+)\""))
            {
                Assert.IsNotNull(BmageSourceCatalog.GetSourceFile(match.Groups["path"].Value), $"The {mode} guide points at '{match.Groups["path"].Value}'.");
            }
        }
    }
}
