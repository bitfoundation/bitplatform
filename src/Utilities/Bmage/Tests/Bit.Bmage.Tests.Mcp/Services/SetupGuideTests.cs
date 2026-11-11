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
            Assert.StartsWith("# Bit.Bmage", guide);
            Assert.Contains("```csharp", guide);
        }
    }

    [TestMethod]
    public void TheBrowserModes_UseTheBrowserRegistration_AndWarnAboutPrerendering()
    {
        foreach (var mode in new[] { "wasm", "server", "auto", "standalone-wasm", "hybrid" })
        {
            var guide = BmageSetupGuide.Get(mode)!;

            Assert.Contains("AddBitBmageServices", guide, mode);
            Assert.Contains("bit-bmage-editor.css", guide, mode);
        }

        foreach (var mode in new[] { "wasm", "server", "auto" })
        {
            Assert.Contains("OnAfterRenderAsync", BmageSetupGuide.Get(mode)!, mode);
        }
    }

    [TestMethod]
    public void TheWebAppModes_RegisterInBothContainers()
    {
        foreach (var mode in new[] { "wasm", "auto" })
        {
            var guide = BmageSetupGuide.Get(mode)!;

            Assert.Contains("BOTH containers", guide, mode);
            Assert.Contains("Client/Program.cs", guide, mode);
            Assert.Contains("Server/Program.cs", guide, mode);
        }
    }

    [TestMethod]
    public void TheWebAssemblyModes_EnableAot()
    {
        foreach (var mode in new[] { "wasm", "auto", "standalone-wasm" })
        {
            Assert.Contains("<RunAOTCompilation>true</RunAOTCompilation>", BmageSetupGuide.Get(mode)!, mode);
        }

        Assert.Contains("RunAOTCompilation` does not apply", BmageSetupGuide.Get("server")!);
    }

    [TestMethod]
    public void TheHeadlessMode_UsesTheHeadlessRegistration_AndRulesOutTheBrowserCodecs()
    {
        var guide = BmageSetupGuide.Get("headless")!;

        Assert.Contains("AddBitBmageHeadlessServices", guide);
        Assert.Contains("processor.Identify(bytes)", guide);
        Assert.Contains("ImageCapabilityException", guide);
        Assert.IsFalse(guide.Contains("bit-bmage-editor.css", StringComparison.Ordinal), "The headless guide tells a server to link a stylesheet.");
        Assert.IsFalse(guide.Contains("ImageFormat.Avif", StringComparison.Ordinal), "The headless guide encodes AVIF, which it cannot.");
        Assert.Contains("Lossless = true", guide);
    }

    [TestMethod]
    public void TheHybridMode_RegistersOnce_AndSavesThroughThePlatform()
    {
        var guide = BmageSetupGuide.Get("hybrid")!;

        Assert.Contains("AddMauiBlazorWebView()", guide);
        Assert.Contains("ImageEnvironment.BrowserServer", guide);
        Assert.Contains("wwwroot/index.html", guide);
        Assert.Contains("Share.Default.RequestAsync", guide);
        Assert.Contains("UseInterpreter", guide);
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
