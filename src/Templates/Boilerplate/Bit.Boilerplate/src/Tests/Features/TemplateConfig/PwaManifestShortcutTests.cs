//+:cnd:noEmit
using System.Reflection;
using System.Text.Json;

namespace Boilerplate.Tests.Features.TemplateConfig;

/// <summary>
/// Every url advertised in the PWA manifest's <c>shortcuts</c> has to be a route the app actually serves.
/// <para>
/// This is invisible during development by construction: manifest shortcuts are the browser's jump list, reachable
/// only after the PWA is installed, so nothing in a normal run - or in any other test in this suite - ever navigates
/// to one. The <c>Profile</c> shortcut pointed at <c>/profile</c> for as long as it took to notice, which is a route
/// that has not existed since the standalone profile page was folded into <c>SettingsPage</c>'s accordion; tapping it
/// landed on "page not found" in 100% of generated projects.
/// </para>
/// <para>
/// The manifest is read as source rather than through a generated project, so every conditional branch in it is
/// checked at once - the <c>offlineDb</c>, <c>sample</c> and <c>module</c> shortcuts included. That works because
/// <c>PageUrls</c>' own conditionals are C# comments too, so the template's own tree compiles every constant.
/// </para>
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class PwaManifestShortcutTests
{
    [TestMethod]
    public void EveryManifestShortcut_Should_PointAtARouteTheAppServes()
    {
        var manifest = ReadTemplateFile("src/Client/Boilerplate.Client.Web/wwwroot/manifest.json");

        // The template engine's .json handling uses `//` comments for its conditionals, so the JSON reader has to skip
        // them - which conveniently leaves every branch's shortcut in the document.
        using var document = JsonDocument.Parse(manifest, new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        });

        Assert.IsTrue(document.RootElement.TryGetProperty("shortcuts", out var shortcuts),
            "manifest.json has no shortcuts array. If shortcuts were removed deliberately, delete this test with them.");

        var knownUrls = KnownPageUrls();
        var checkedAny = false;

        foreach (var shortcut in shortcuts.EnumerateArray())
        {
            var url = shortcut.GetProperty("url").GetString()!;
            var name = shortcut.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : url;
            checkedAny = true;

            Assert.Contains(url, knownUrls,
                $"The manifest's \"{name}\" shortcut points at {url}, which is not a route this app serves. " +
                $"An installed PWA offers it in the jump list and it lands on the not-found page. Known routes: " +
                $"{string.Join(", ", knownUrls.OrderBy(u => u, StringComparer.Ordinal))}");
        }

        Assert.IsTrue(checkedAny, "The shortcuts array is empty, so this test asserted nothing.");
    }

    /// <summary>
    /// Every route constant on <see cref="PageUrls"/>, plus the <c>/settings/{Section}</c> urls that
    /// <c>SettingsPage</c>'s optional route segment serves - which is where the profile, account, two-factor and
    /// session screens actually live.
    /// </summary>
    private static HashSet<string> KnownPageUrls()
    {
        var urls = typeof(PageUrls)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var section in typeof(PageUrls.SettingsSections).GetFields(BindingFlags.Public | BindingFlags.Static)
                                                                 .Where(field => field.FieldType == typeof(string)))
        {
            urls.Add($"{PageUrls.Settings}/{(string)section.GetValue(null)!}");
        }

        return urls;
    }

    private static string ReadTemplateFile(string relativePath)
    {
        var path = Path.Combine(GetTemplateRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));

        Assert.IsTrue(File.Exists(path), $"{relativePath} does not exist under the template root. If the file moved, " +
                                         "point this test at its new home rather than deleting the assertion.");

        return File.ReadAllText(path);
    }

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
