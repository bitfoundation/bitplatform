using Bit.BevTools;

namespace Bit.BevTools.Samples.Lazy.Client.DevTools;

/// <summary>
/// The app's BevTools configuration, in one place for both ways BevTools runs here: registered at startup in
/// Development, and attached on demand by the "Open dev tools" button everywhere else. Program.cs hands it to the
/// loader (BevToolsLoaderOptions.ConfigureBevTools) through a lambda, so the startup code names no Bit.BevTools type and
/// this method runs - and Bit.BevTools.wasm is needed - only once BevTools is loaded.
/// </summary>
/// <remarks>
/// Public, so the server's Program.cs shares it: the router lists the public types of this assembly while it starts,
/// before Bit.BevTools.wasm is downloaded, and that is safe for a type that only names Bit.BevTools types in its members'
/// signatures and bodies. A public type whose base type or interface lives in Bit.BevTools is not: it fails to load
/// there, which is why the extension, its command and the session panel are internal.
/// </remarks>
public static class SampleDevTools
{
    /// <summary>The app's own panel, for BevToolsLoader.OpenAsync(panelId).</summary>
    public const string SupportPanelId = "sample-support";

    public static void Configure(BevToolsOptions options)
    {
        // A plain component of the app: it needs nothing of BevTools, so it may be a public .razor component here.
        options.AddPanel<SampleSupportPanel>(SupportPanelId, "Support");

        // An extension: a panel that reads the session (it inherits BevToolsTab) and a command for the palette.
        options.Extensions.Add(new SampleBevToolsExtension());

        // The app's own secrets, on top of BevTools' defaults.
        options.SensitiveNamePatterns.Add("handle");
    }
}
