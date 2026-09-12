//+:cnd:noEmit
using System.Text.RegularExpressions;

namespace Boilerplate.Tests.Features.TemplateConfig;

/// <summary>
/// A document has ONE HeadOutlet, and only the LAST <c>HeadContent</c> rendered into it survives - the earlier ones
/// are not merged, they are dropped. So the moment a second component opens a <c>HeadContent</c> of its own, whatever
/// the app put in the head stops being there: the Content-Security-Policy meta tag, the App Insights snippet, the
/// page's canonical url. Nothing errors, nothing warns, no build and no test notices, and the loss shows up as
/// "the CSP is not applied on this one page" months later.
/// <para>
/// That is why every app-wide head tag goes through <c>AppHead</c>, which <c>AppHeadCoordinator</c> renders
/// inside the single HeadContent it owns, and where it also
/// hosts a <c>SectionOutlet</c> for <c>AppPageData.HeadSectionName</c>, and why a page contributes head tags with
/// <c>&lt;AppPageData Head="..." /&gt;</c> or a <c>SectionContent</c> for that section - never with a HeadContent.
/// Sections compose; HeadContent does not.
/// </para>
/// <para>
/// A source scan rather than a rendering test, on purpose: the rule is a property of the source text and has to hold
/// for every page, layout and render mode at once, while rendering proves one of them.
/// </para>
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class HeadContentUsageTests
{
    /// <summary>
    /// The two components allowed to open a HeadContent, by file name - a generated project renames the projects
    /// around them. <c>AppHeadCoordinator</c> is the app-wide one; <c>SsrLayout</c> is its counterpart for
    /// <c>BlazorMode: BlazorSsr</c> and <c>[ExcludeFromInteractiveRouting]</c> pages, where MainLayout - and with it
    /// AppHeadCoordinator - never renders. The two are mutually exclusive, so the document still only ever has one.
    /// </summary>
    private static readonly string[] AllowedFileNames = ["AppHeadCoordinator.razor", "SsrLayout.razor"];

    [TestMethod]
    public void NoComponent_Should_OpenItsOwnHeadContent()
    {
        var root = GetSolutionRoot();
        var razorFiles = EnumerateRazorFiles(root).ToArray();

        Assert.IsGreaterThan(50, razorFiles.Length, "Almost no razor files were found, so this test would pass vacuously.");

        var offenders = new List<string>();
        var allowedHits = new HashSet<string>();

        foreach (var file in razorFiles)
        {
            var markup = StripComments(File.ReadAllText(file));

            foreach (Match match in Regex.Matches(markup, @"<HeadContent\b"))
            {
                var fileName = Path.GetFileName(file);

                if (AllowedFileNames.Contains(fileName))
                {
                    allowedHits.Add(fileName);
                    continue;
                }

                var lineNumber = markup.Take(match.Index).Count(c => c == '\n') + 1;

                offenders.Add($"{Path.GetRelativePath(root, file)}:{lineNumber}");
            }
        }

        Assert.IsEmpty(offenders,
            $"A component opened a HeadContent of its own. Only the LAST HeadContent rendered into the document's " +
            $"single HeadOutlet survives, so this one silently drops everything {AllowedFileNames[0]} puts in the " +
            $"head - the CSP meta tag included - on every page that renders it. Render into the head section " +
            $"instead: <AppPageData Head=\"...\" /> for a page, or <SectionContent " +
            $"SectionName=\"@AppPageData.HeadSectionName\"> anywhere else.{Environment.NewLine}" +
            $"{string.Join(Environment.NewLine, offenders)}");

        // Without this the test would keep passing after someone removes the app's own HeadContent, at which point
        // the head is empty rather than shadowed - the same defect, arriving from the other side.
        foreach (var allowed in AllowedFileNames)
        {
            Assert.Contains(allowed, allowedHits,
                $"{allowed} no longer opens a HeadContent. Head tags reach the document through it, so either " +
                $"nothing renders into the head any more, or the component that now owns it belongs on this list.");
        }
    }

    /// <summary>
    /// Razor and HTML comments describing the rule mention the tag by name (this test class is named in one of
    /// them), and a commented-out HeadContent is not one.
    /// </summary>
    private static string StripComments(string markup)
    {
        markup = Regex.Replace(markup, @"@\*.*?\*@", string.Empty, RegexOptions.Singleline);

        return Regex.Replace(markup, @"<!--.*?-->", string.Empty, RegexOptions.Singleline);
    }

    private static IEnumerable<string> EnumerateRazorFiles(string root)
    {
        return Directory.EnumerateFiles(Path.Combine(root, "src"), "*.razor", SearchOption.AllDirectories)
                        .Where(f => f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") is false)
                        .Where(f => f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") is false);
    }

    /// <summary>
    /// Walks up from the test binaries to the directory that owns the solution and its <c>src</c> folder. Unlike the
    /// <c>.template.config</c> anchor the template-only tests use, this one also resolves in a generated project,
    /// where this test still has to run.
    /// </summary>
    private static string GetSolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null &&
               (Directory.Exists(Path.Combine(directory.FullName, "src")) is false ||
                Directory.EnumerateFiles(directory.FullName, "*.sln*").Any() is false))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            Assert.Inconclusive("No solution file with a src folder above the test binaries, so there are no razor sources to scan.");
            return default!;
        }

        return directory.FullName;
    }
}
