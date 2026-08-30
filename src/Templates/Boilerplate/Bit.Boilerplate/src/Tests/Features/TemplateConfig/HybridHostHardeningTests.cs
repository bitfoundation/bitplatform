//+:cnd:noEmit
using System.Text.RegularExpressions;

namespace Boilerplate.Tests.Features.TemplateConfig;

/// <summary>
/// Guards a handful of decisions in the blazor hybrid heads that are one line each, invisible at runtime, and
/// expensive to get wrong. None of them can be caught by building or by running the app: an over-broad permission
/// grant, a committed signing key, an unrepresentable version, an unvalidated deep link and a push subscription with
/// no channel all behave exactly like the correct thing right up until they don't.
/// <para>
/// Source scans, not rendering or integration tests, on purpose. Each property being defended is a property of the
/// source text and holds for all 22 template configurations, while a rendering test would prove one configuration on
/// one platform, and the platforms that matter here (android, ios, the WinUI head) are not ones this suite can drive
/// at all. The scanned files carry no template conditional, and the only ones <c>template.json</c> excludes are the
/// three <c>IPushNotificationService</c> implementations, dropped under <c>notification != true</c>; that costs the
/// scan nothing, because <see cref="GetTemplateRoot"/> anchors on <c>.template.config/template.json</c>, which is
/// itself excluded from the output, so these paths always resolve inside the template's own complete tree - or the
/// whole class goes inconclusive because there is no such anchor above the binaries.
/// </para>
/// <para>
/// <b>Deliberately not guarded: web view developer tools.</b> <c>AddBlazorWebViewDeveloperTools()</c> is
/// unconditional in both hybrid heads, and MAUI's own <c>BlazorWebViewHandler</c> reads <c>DeveloperTools.Enabled</c>
/// to call <c>WebView.SetWebContentsDebuggingEnabled</c> on android and to set <c>inspectable</c> on ios 16.4+. A
/// released, store-signed build is therefore attachable from <c>chrome://inspect</c> or Safari's Web Inspector, which
/// gives anyone with an authorized device connection a javascript console inside the signed-in page's own origin.
/// That is the maintainer's call, and the trade is that a Release-configuration build stays debuggable on a real
/// device, which is how these apps are actually diagnosed. Guard tests for it were written, run against a control,
/// and then removed - a test that asserts the opposite of the shipped decision is worse than no test. If the decision
/// is ever revisited, the assertion is: <c>AddBlazorWebViewDeveloperTools()</c> inside a positive
/// <c>if (AppEnvironment.IsDevelopment())</c> in <c>MauiProgram.Services.cs</c> and
/// <c>Client.Windows/Program.Services.cs</c>, and no literal <c>true</c> passed to
/// <c>SetWebContentsDebuggingEnabled</c> or <c>Inspectable</c> in <c>MauiProgram.cs</c>.
/// </para>
/// <para>
/// <b>Deliberately not guarded: the MSIX identity version, and the two <c>.well-known</c> files.</b> Guard tests for
/// both were written and control-run, then removed when the maintainer reverted the changes they defended. The MAUI
/// windows head takes <c>$(ApplicationVersion)</c> - an android versionCode - as the fourth component of its MSIX
/// <c>&lt;Identity Version&gt;</c>, so the default ships as <c>1.0.0.10000</c> and any <c>Version</c> from
/// <c>6.56.0</c> up exceeds the 65535 per-component cap in <c>AppxManifestTypes.xsd</c>; and
/// <c>wwwroot/.well-known/assetlinks.json</c> and <c>apple-app-site-association</c> ship bitplatform's own store
/// package names, apple team id and release signing fingerprint rather than placeholders, so a generated app's
/// <c>AutoVerify</c> app links do not verify until the developer replaces them by hand. Both are the maintainer's
/// call. If either is revisited, the assertions were: a <c>$(TargetFramework.Contains('-windows'))</c> PropertyGroup
/// setting <c>&lt;ApplicationVersion&gt;0&lt;/ApplicationVersion&gt;</c> in the MAUI csproj, and no
/// <c>com.bitplatform.</c> substring or real <c>AA:BB:..</c> SHA-256 fingerprint in either <c>.well-known</c> file.
/// </para>
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class HybridHostHardeningTests
{
    /// <summary>
    /// WebView2's <c>PermissionRequested</c>: answering every kind with <c>Allow</c> and setting <c>Handled</c>
    /// suppresses WebView2's own prompt, so camera, geolocation and clipboard-read are granted with no ui to any
    /// script running in the page - including third party scripts, such as the ad SDK, which run in the app's own
    /// origin. Both heads did exactly that.
    /// <para>
    /// The android head has always taken the opposite position, deliberately and with a comment saying why
    /// (<c>AppWebChromeClient.OnPermissionRequest</c>: an allow list of one, widened a resource at a time). This test
    /// exists because the two WebView2 heads are the ones that drifted from it, and nothing about the drift is visible
    /// while the app works.
    /// </para>
    /// </summary>
    [TestMethod]
    [DataRow("src/Client/Boilerplate.Client.Maui/MauiProgram.cs")]
    [DataRow("src/Client/Boilerplate.Client.Windows/Program.cs")]
    public void WebView2PermissionRequests_Should_BeAnsweredPerKind(string relativePath)
    {
        var source = ReadTemplateFile(relativePath);

        var grant = source.IndexOf("CoreWebView2PermissionState.Allow", StringComparison.Ordinal);

        Assert.AreNotEqual(-1, grant,
            $"{relativePath} no longer grants any WebView2 permission. If the handler was removed on purpose, delete " +
            "this DataRow - but note that dictation needs the microphone on the WinUI and WinForms heads.");

        // The kind test has to come before the grant, not merely exist in the file: a switch placed after it would
        // already have allowed everything.
        var preceding = source[..grant];
        var kindCheck = preceding.LastIndexOf("PermissionKind", StringComparison.Ordinal);

        Assert.AreNotEqual(-1, kindCheck,
            $"{relativePath} sets CoreWebView2PermissionState.Allow without ever inspecting args.PermissionKind, so " +
            "every kind WebView2 asks about - Camera, Geolocation, ClipboardRead, FileReadWrite, WindowManagement - is " +
            "granted silently, and setting Handled suppresses the prompt that would otherwise have asked the user. " +
            "Allow only the kinds the app actually uses and leave the rest at CoreWebView2PermissionState.Default.");

        // ...and it has to CONTROL the grant, not merely precede it. Reading args.PermissionKind into a log line and
        // then allowing everything anyway would satisfy the assertion above while changing nothing, so what is
        // required is the early-out between the two: every kind that is not on the allow list leaves the handler
        // before Handled/Allow are ever set, which is what preserves WebView2's own prompt for it.
        Assert.Contains("return", preceding[kindCheck..],
            $"{relativePath} inspects args.PermissionKind before granting, but nothing returns between the check and " +
            "CoreWebView2PermissionState.Allow - so the check does not gate the grant and every kind is still allowed. " +
            "Use an allow list that returns early for everything else, leaving those requests at Default.");
    }

    /// <summary>
    /// Both pipelines write the android release keystore into <c>src/Client/Boilerplate.Client.Maui/</c>, and
    /// <c>.docs/16</c> tells a developer reproducing a signed build locally to put their own there. A `git add .` then
    /// commits the play store upload key, which cannot be quietly rotated once it is public - recovery needs a
    /// Google-assisted key reset.
    /// </summary>
    [TestMethod]
    [DataRow("*.keystore")]
    [DataRow("*.jks")]
    [DataRow("*.p12")]
    [DataRow("*.pfx")]
    public void GitIgnore_Should_CoverSigningMaterial(string pattern)
    {
        var gitIgnore = ReadTemplateFile(".gitignore");

        Assert.Contains(line => line.Trim() == pattern, gitIgnore.Split('\n'),
            $".gitignore has no '{pattern}' rule. cd-template.yml and .azure-devops/workflows/cd.yml both materialise " +
            "Boilerplate.keystore into a tracked source directory, and the documented local workflow puts a real one " +
            "at the same path.");
    }

    /// <summary>
    /// The android <c>versionCode</c> is derived from <c>$(Version)</c> by a regex in the MAUI csproj. Anchoring that
    /// regex at the end - which is how it shipped - makes <c>1.0.3.4</c> and <c>1.0.3-rc1</c> fail to match, and the
    /// csproj turns a miss into patch <c>0</c> <b>silently</b>. Two consecutive releases then produce the same
    /// versionCode, and the only symptom is a store-side rejection with nothing in the build log pointing at the
    /// csproj.
    /// <para>
    /// This asserts against the pattern the csproj actually ships, read out of the file, rather than a copy of it -
    /// otherwise the test pins its own duplicate and the csproj is free to regress underneath it.
    /// </para>
    /// </summary>
    [TestMethod]
    [DataRow("1.0.0", 0)]
    [DataRow("1.0.7", 7)]
    [DataRow("1.0", 0)]
    public void MauiVersionCode_Should_ReadThePatchOfEveryAcceptedVersionShape(string version, int expectedPatch)
    {
        var csproj = ReadTemplateFile("src/Client/Boilerplate.Client.Maui/Boilerplate.Client.Maui.csproj");

        var declaration = Regex.Match(csproj, @"<_PatchVersionString>.*?Regex\]::Match\(\$\(Version\),\s*'(?<pattern>[^']+)'\)");

        Assert.IsTrue(declaration.Success,
            "Could not find the _PatchVersionString regex in Boilerplate.Client.Maui.csproj. If the versionCode " +
            "derivation was rewritten, rewrite this test against the new shape rather than deleting it.");

        var shipped = declaration.Groups["pattern"].Value;
        var match = Regex.Match(version, shipped);
        var patch = match.Success && match.Groups[1].Value.Length > 0 ? int.Parse(match.Groups[1].Value) : 0;

        Assert.AreEqual(expectedPatch, patch,
            $"The shipped pattern '{shipped}' reads patch {patch} out of Version '{version}', not {expectedPatch}. " +
            "A wrong patch is not a build failure - it becomes a versionCode the store has already seen, so the next " +
            "upload is rejected or, worse, accepted as an update no device ever receives.");
    }

    /// <summary>
    /// The other half, and the one that actually closes the defect. <c>versionCode</c> is
    /// <c>major*10000 + minor*100 + patch</c> — it has nowhere to put a fourth component or a pre-release label, so
    /// <c>1.0.0.6</c> and <c>1.0.0.7</c> both compute to <b>10000</b>, which is the exact collision the finding
    /// described. Unanchoring the patch regex did <b>not</b> fix that: it only helps when the patch itself is
    /// non-zero. The build therefore has to refuse a Version it cannot represent, rather than quietly discard part of
    /// it, which is also this project's stated preference for a misconfiguration.
    /// </summary>
    [TestMethod]
    [DataRow("1.0.0", true)]
    [DataRow("1.0", true)]
    [DataRow("10.20.30", true)]
    [DataRow("1.0.0.4", false)]
    [DataRow("1.0.0-rc1", false)]
    [DataRow("1", false)]
    public void MauiVersionCode_Should_RefuseAVersionItCannotRepresent(string version, bool expectedToBuild)
    {
        var csproj = ReadTemplateFile("src/Client/Boilerplate.Client.Maui/Boilerplate.Client.Maui.csproj");

        var validation = Regex.Match(csproj, @"IsMatch\(\$\(Version\),\s*`(?<pattern>[^`]+)`\)");

        Assert.IsTrue(validation.Success,
            "Could not find the Version validation condition in Boilerplate.Client.Maui.csproj. Without it a Version " +
            "carrying a revision or a pre-release label is silently truncated into a colliding versionCode.");

        var accepted = Regex.IsMatch(version, validation.Groups["pattern"].Value);

        Assert.AreEqual(expectedToBuild, accepted,
            expectedToBuild
                ? $"Version '{version}' is a shape the versionCode represents exactly, but the build refuses it."
                : $"Version '{version}' is accepted by the build, yet the versionCode cannot represent it - the extra " +
                  "component is discarded and the app ships with a versionCode another build has already used.");
    }

    /// <summary>
    /// <c>Routes.OpenUniversalLink</c> is the sink every url that arrives from outside the app flows into: an android
    /// deep-link intent, an apple universal link, and the push-notification tap handlers on every hybrid head. None of
    /// those callers can vouch for what they were handed - android's <c>MainActivity</c> is exported, so any app on
    /// the device can send it an explicit intent - and the url ends up in <c>NavigationManager.NavigateTo</c>.
    /// <para>
    /// The specific escape is that a network-path reference survives every "it's relative" intuition:
    /// <c>new java.net.URL("https://x//evil.example/p").getFile()</c> returns <c>//evil.example/p</c>, which
    /// <c>NavigateTo</c> resolves protocol-relative to <c>https://evil.example/p</c> and BlazorWebView then opens in
    /// the system browser. <c>Uri.IsAppRelativeUrl</c> exists precisely to reject that shape and is applied at every
    /// other <c>NavigateTo</c> sink in the repo; this one was the remainder.
    /// </para>
    /// </summary>
    [TestMethod]
    public void UniversalLinkNavigation_Should_BeGuardedByAnAppRelativeUrlCheck()
    {
        var source = ReadTemplateFile("src/Client/Boilerplate.Client.Core/Components/Routes.razor.cs");

        var navigate = source.IndexOf("navigationManager.NavigateTo(", StringComparison.Ordinal);

        Assert.AreNotEqual(-1, navigate,
            "Routes.razor.cs no longer calls NavigationManager.NavigateTo. If OpenUniversalLink was rewritten, point " +
            "this test at the new sink rather than deleting it - the callers are still untrusted.");

        // Presence anywhere in the file is not enough: the check has to run BEFORE the navigation, or the escape has
        // already happened by the time it is evaluated.
        var preceding = source[..navigate];

        Assert.IsTrue(preceding.Contains("IsAppRelativeUrl", StringComparison.Ordinal),
            "Routes.OpenUniversalLink navigates to its url without calling Uri.IsAppRelativeUrl first. Every caller " +
            "of this method is an untrusted external channel - an exported android activity's intent data and the " +
            "push-notification pageUrl payload on three platforms - and a network-path reference such as " +
            "'//evil.example/p' is a well-formed RELATIVE url that NavigateTo resolves off this app's origin.");
    }

    /// <summary>
    /// All three <c>IPushNotificationService</c> implementations carry a <c>[mirror]</c> header naming the other two,
    /// and the apple pair drifted from it: after the 15-second token wait times out they log and then <b>fall
    /// through</b> to return a <c>PushNotificationSubscriptionDto</c> whose <c>PushChannel</c> is null, where android
    /// returns null and lets <c>Subscribe</c> exit quietly.
    /// <para>
    /// The server rejects that body with a <c>BadRequestException</c> (its own guard, added earlier in this review),
    /// which the client renders as an <i>Interrupting</i> dialog and which aborts <c>PropagateAuthState</c> before it
    /// records the propagated user - so it recurs. Nothing about it is visible on a machine that gets its APNS token,
    /// which is every developer machine.
    /// </para>
    /// </summary>
    [TestMethod]
    [DataRow("src/Client/Boilerplate.Client.Maui/Platforms/iOS/Services/iOSPushNotificationService.cs")]
    [DataRow("src/Client/Boilerplate.Client.Maui/Platforms/MacCatalyst/Services/MacCatalystPushNotificationService.cs")]
    [DataRow("src/Client/Boilerplate.Client.Maui/Platforms/Android/Services/AndroidPushNotificationService.cs")]
    public void PushSubscription_Should_NotBeReturnedWhenTheTokenNeverArrived(string relativePath)
    {
        var source = ReadTemplateFile(relativePath);

        var catchBlock = source.IndexOf("catch (Exception exp)", StringComparison.Ordinal);

        Assert.AreNotEqual(-1, catchBlock,
            $"{relativePath} no longer catches the token-wait timeout in GetSubscription. If the wait was replaced " +
            "with something that cannot time out, rewrite this test against it rather than deleting it.");

        // The DTO is constructed after the catch in every one of the three files, so "does the catch return before
        // reaching it" is exactly the property that separates the android behaviour from the apple one.
        var dto = source.IndexOf("new PushNotificationSubscriptionDto", StringComparison.Ordinal);

        Assert.AreNotEqual(-1, dto, $"{relativePath} no longer builds a PushNotificationSubscriptionDto.");
        Assert.IsLessThan(dto, catchBlock, $"{relativePath} builds the subscription before the token wait, which this test cannot reason about.");

        Assert.IsTrue(source[catchBlock..dto].Contains("return null", StringComparison.Ordinal),
            $"{relativePath} falls through its token-wait catch into the PushNotificationSubscriptionDto, so a device " +
            "that never received a push token posts a subscription whose PushChannel is null. The server rejects that " +
            "with a 400, which reaches the user as a blocking dialog and aborts the auth-state propagation that asked " +
            "for it. Return null instead, as the [mirror] siblings do.");
    }

    private static string ReadTemplateFile(string relativePath)
    {
        var path = Path.Combine(GetTemplateRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));

        Assert.IsTrue(File.Exists(path), $"{relativePath} does not exist under the template root. If the file moved, " +
                                         "point this test at its new home rather than deleting the assertion.");

        return File.ReadAllText(path);
    }

    /// <summary>
    /// Walks up from the test assembly to the directory that owns <c>.template.config/template.json</c>, which is the
    /// same anchor <c>SourceNameLeakTests</c> and <c>TemplateConfigurationTests</c> use.
    /// </summary>
    private static string GetTemplateRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null &&
               File.Exists(Path.Combine(directory.FullName, ".template.config", "template.json")) is false)
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            Assert.Inconclusive("No .template.config/template.json above the test binaries - this is a generated project, not the template's own tree.");
            return default!;
        }

        return directory.FullName;
    }
}
