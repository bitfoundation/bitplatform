//+:cnd:noEmit
using System.Text.RegularExpressions;

namespace Boilerplate.Tests.Features.TemplateConfig;

/// <summary>
/// Guards three decisions in the two blazor hybrid heads that are one line each, invisible at runtime, and expensive
/// to get wrong. None of them can be caught by building or by running the app: a debuggable web view, an over-broad
/// permission grant and a committed signing key all behave exactly like the correct thing right up until they don't.
/// <para>
/// Source scans, not rendering or integration tests, on purpose. Each property being defended is a property of the
/// source text and holds for all 22 template configurations - none of these files carries a template conditional and
/// none is excluded by <c>template.json</c> - while a rendering test would prove one configuration on one platform, and the
/// platforms that matter here (android, ios, the WinUI head) are not ones this suite can drive at all.
/// </para>
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class HybridHostHardeningTests
{
    /// <summary>
    /// <c>AddBlazorWebViewDeveloperTools()</c> is what makes a released app's web view inspectable, and neither the
    /// compiler nor the app says so.
    /// <para>
    /// The review filed this as one stray <c>SetWebContentsDebuggingEnabled(true)</c> in <c>MauiProgram</c>. That was
    /// wrong, and decompiling the shipping framework assembly is what showed it: MAUI's own
    /// <c>BlazorWebViewHandler.CreatePlatformView</c> already calls
    /// <c>WebView.SetWebContentsDebuggingEnabled(DeveloperTools.Enabled)</c> on android and already sets
    /// <c>inspectable</c> on ios 16.4+, from the same flag. So gating the line the finding named would have changed
    /// nothing at all - the exposure lives in the <c>services.AddBlazorWebViewDeveloperTools()</c> call, which was
    /// unconditional in both heads. This test pins the corrected fact rather than the finding's first guess, which is
    /// the whole reason it is worth its runtime.
    /// </para>
    /// </summary>
    [TestMethod]
    [DataRow("src/Client/Boilerplate.Client.Maui/MauiProgram.Services.cs")]
    [DataRow("src/Client/Boilerplate.Client.Windows/Program.Services.cs")]
    public void BlazorWebViewDeveloperTools_Should_OnlyBeEnabledInDevelopment(string relativePath)
    {
        var source = ReadTemplateFile(relativePath);

        var call = source.IndexOf("AddBlazorWebViewDeveloperTools", StringComparison.Ordinal);

        Assert.AreNotEqual(-1, call,
            $"{relativePath} no longer calls AddBlazorWebViewDeveloperTools. If that is deliberate, delete this DataRow; " +
            "if the call moved, this test has stopped guarding anything.");

        // The guard has to be the enclosing statement, so look back rather than anywhere in the file: an
        // AppEnvironment.IsDevelopment() somewhere else entirely would satisfy a whole-file search and prove nothing.
        var preceding = source[..call];
        var guard = preceding.LastIndexOf("AppEnvironment.IsDevelopment()", StringComparison.Ordinal);

        Assert.AreNotEqual(-1, guard,
            $"{relativePath} calls AddBlazorWebViewDeveloperTools with no AppEnvironment.IsDevelopment() guard before it. " +
            "That turns DeveloperTools.Enabled on in release, and MAUI's BlazorWebViewHandler then makes the web view " +
            "inspectable from chrome://inspect (android) or Safari's Web Inspector (ios) - a javascript console inside " +
            "the signed-in page's own origin, on a store build.");

        var between = preceding[guard..];

        Assert.IsFalse(between.Contains('}'),
            $"{relativePath} has an AppEnvironment.IsDevelopment() before AddBlazorWebViewDeveloperTools, but a block " +
            "closes between the two, so the call is not actually inside the guard.");
    }

    /// <summary>
    /// The redundant restatements of the same switch in <c>MauiProgram.SetupBlazorWebView</c>. They are not the cause
    /// of the exposure - see the test above - but a hard-coded <c>true</c> in either of them would put it back
    /// regardless of what the DI registration decided, which is a strictly worse failure than the original because the
    /// fix would look like it was already applied.
    /// </summary>
    [TestMethod]
    public void MauiProgram_Should_NotHardCodeWebViewDebuggingOn()
    {
        var source = ReadTemplateFile("src/Client/Boilerplate.Client.Maui/MauiProgram.cs");

        StringAssert.DoesNotMatch(source, new Regex(@"SetWebContentsDebuggingEnabled\(\s*true\s*\)"),
            "MauiProgram hard-codes android web view debugging on. Pass AppEnvironment.IsDevelopment() instead - " +
            "MAUI's own handler already made this call from DeveloperTools.Enabled, so a literal true here silently " +
            "overrides the release decision.");

        StringAssert.DoesNotMatch(source, new Regex(@"Inspectable\s*=\s*true"),
            "MauiProgram hard-codes WKWebView Inspectable on, which makes a released ios build attachable from " +
            "Safari's Web Inspector. Pass AppEnvironment.IsDevelopment() instead.");
    }

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
        var kindCheck = source[..grant].LastIndexOf("PermissionKind", StringComparison.Ordinal);

        Assert.AreNotEqual(-1, kindCheck,
            $"{relativePath} sets CoreWebView2PermissionState.Allow without ever inspecting args.PermissionKind, so " +
            "every kind WebView2 asks about - Camera, Geolocation, ClipboardRead, FileReadWrite, WindowManagement - is " +
            "granted silently, and setting Handled suppresses the prompt that would otherwise have asked the user. " +
            "Allow only the kinds the app actually uses and leave the rest at CoreWebView2PermissionState.Default.");
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

        Assert.IsTrue(gitIgnore.Split('\n').Any(line => line.Trim() == pattern),
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
    [DataRow("1.0.7.4", 7)]
    [DataRow("1.0.7-rc1", 7)]
    public void MauiVersionCode_Should_ReadThePatchOfEveryVersionShapeCiPassesIn(string version, int expectedPatch)
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
