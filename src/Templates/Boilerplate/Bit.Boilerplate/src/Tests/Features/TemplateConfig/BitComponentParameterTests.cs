//+:cnd:noEmit
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace Boilerplate.Tests.Features.TemplateConfig;

/// <summary>
/// Guards the one defect this review has now found in three consecutive batches: markup that passes a parameter the
/// Bit.BlazorUI component does not have.
/// <para>
/// <c>BitComponentBase.SetParametersAsync</c> does not throw on an unknown parameter - its <c>default:</c> arm files
/// the name under <c>HtmlAttributes</c>, and the component splats that onto its root element. So the feature the
/// author asked for is simply off, a PascalCase attribute name leaks into the DOM, and <b>nothing errors, nothing
/// warns, and no build or test notices</b>. The three found so far:
/// <list type="bullet">
/// <item><c>EnableVirtualization</c> on <c>BitBasicList</c> - the parameter is <c>Virtualize</c>. Found on
/// <c>AppDiagnosticModal</c> first, then on <b>both</b> todo pages and <c>SessionsSection</c>, because the first fix
/// was never swept across the other call sites. Three sites, two batches apart.</item>
/// <item><c>Accent</c> on <c>BitSearchBox</c> - the parameter is <c>Background</c>. <c>Accent</c> is real on
/// <c>BitTextField</c>, <c>BitOtpInput</c>, <c>BitNumberField</c>, <c>BitNav</c>, <c>BitCarousel</c> and
/// <c>BitSwiper</c>, which is almost certainly where the name came from.</item>
/// <item><c>ChildContent</c> on <c>BitColorPicker</c>, which has none - a string translated into nine locales
/// rendered nowhere at all.</item>
/// </list>
/// </para>
/// <para>
/// Each name is checked by <b>reflection over the pinned Bit.BlazorUI assemblies</b> rather than by grepping for the
/// string, which buys two things a text search cannot. It fails if a future package version removes or renames the
/// replacement parameter (<c>Virtualize</c>, <c>Background</c>) - the same silent-switch-off, arriving through an
/// upgrade instead of a typo. And it stops asserting the moment a package version legitimately <i>adds</i> one of the
/// wrong names, so the test cannot outlive its own premise.
/// </para>
/// <para>
/// Deliberately a narrow, named list rather than a general "every PascalCase attribute must name a real parameter"
/// scan. That general form was written first and produced 22 hits, of which most were <c>Classes="@(new() { Root =
/// ... })"</c> object initialisers rather than component attributes - a regex cannot tell the two apart without
/// parsing Razor, and a guard that has to be taught about false positives is a guard nobody trusts. The residue of
/// that experiment (<c>BitIcon Title</c>, <c>BitPersona Image</c>) is recorded as a review lead instead of being
/// half-asserted here.
/// </para>
/// <para>
/// A source scan, not a rendering test, on purpose: the property being defended is a property of the source text and
/// holds for all 22 configurations, while rendering proves one.
/// </para>
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class BitComponentParameterTests
{
    /// <summary>
    /// A wrong parameter name, the component it was written on, and the parameter that was meant. Every entry is a
    /// defect that actually shipped in this repository.
    /// </summary>
    private static IEnumerable<(string Component, string WrongName, string RealName)> KnownWrongParameters =>
    [
        ("BitBasicList", "EnableVirtualization", "Virtualize"),
        ("BitSearchBox", "Accent", "Background"),
        ("BitColorPicker", "ChildContent", "Label"),
    ];

    [TestMethod]
    public void NoRazorMarkup_Should_PassAParameterTheBitComponentDoesNotHave()
    {
        var root = GetTemplateRoot();
        var razorFiles = EnumerateRazorFiles(root).ToArray();

        Assert.IsGreaterThan(50, razorFiles.Length, "Almost no razor files were found, so this test would pass vacuously.");

        var offenders = new List<string>();

        foreach (var (component, wrongName, realName) in KnownWrongParameters)
        {
            var componentType = ResolveBitComponent(component);

            Assert.IsNotNull(componentType,
                $"{component} is not in the referenced Bit.BlazorUI assemblies any more. Either it was renamed - in " +
                $"which case every call site needs revisiting - or this test is now checking a component that does " +
                $"not exist.");

            Assert.IsTrue(HasParameter(componentType, realName),
                $"{component}.{realName} no longer exists in the pinned package, so the call sites this test protects " +
                $"are silently doing nothing again - exactly the defect it was written for, arriving through a " +
                $"package upgrade rather than a typo.");

            if (HasParameter(componentType, wrongName))
            {
                // The package added it. Nothing to defend any more; drop the row rather than assert a stale claim.
                continue;
            }

            // `<BitBasicList ... EnableVirtualization ...>` - the name may be a bare boolean attribute with no `=`,
            // which is how it was written at all three sites, so the pattern must not require one.
            var pattern = new Regex($@"<{Regex.Escape(component)}\b[^<>]*?\b{Regex.Escape(wrongName)}\b", RegexOptions.Compiled | RegexOptions.Singleline);

            foreach (var file in razorFiles)
            {
                var text = File.ReadAllText(file);

                foreach (Match match in pattern.Matches(text))
                {
                    var lineNumber = text.Take(match.Index).Count(c => c == '\n') + 1;

                    offenders.Add($"{Path.GetRelativePath(root, file)}:{lineNumber} -> <{component} {wrongName} ...> (the parameter is {realName})");
                }
            }
        }

        Assert.IsEmpty(offenders,
            $"A Bit component was passed a parameter it does not declare. BitComponentBase does not throw for this - " +
            $"it routes the name into HtmlAttributes and splats it onto the root element, so the feature is silently " +
            $"off and the PascalCase name ends up in the DOM. Use the parameter named at the end of each line." +
            $"{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    /// <summary>
    /// True when the type - or any base up the chain - declares a public <c>[Parameter]</c> or
    /// <c>[CascadingParameter]</c> with this name.
    /// </summary>
    private static bool HasParameter(Type componentType, string name)
    {
        var property = componentType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);

        if (property is null) return false;

        return property.IsDefined(typeof(ParameterAttribute), inherit: true) ||
               property.IsDefined(typeof(CascadingParameterAttribute), inherit: true);
    }

    /// <summary>
    /// Finds a component by the name markup writes, in the Bit.BlazorUI assemblies the project actually references -
    /// so the check runs against the pinned package rather than a newer working tree.
    /// </summary>
    private static Type? ResolveBitComponent(string name)
    {
        // Touching a type from each assembly guarantees it is loaded before the AppDomain is enumerated.
        _ = typeof(Bit.BlazorUI.BitButton);
        _ = typeof(Bit.BlazorUI.BitDataGrid<>);

        return AppDomain.CurrentDomain
                        .GetAssemblies()
                        .Where(a => a.GetName().Name?.StartsWith("Bit.BlazorUI", StringComparison.Ordinal) is true)
                        .SelectMany(a => a.GetExportedTypes())
                        .Where(t => typeof(IComponent).IsAssignableFrom(t) && t.IsAbstract is false)
                        // A generic component is written `<BitDataGrid ...>` in markup, so match on the bare name.
                        .FirstOrDefault(t => t.Name.Split('`')[0] == name);
    }

    private static IEnumerable<string> EnumerateRazorFiles(string root)
    {
        return Directory.EnumerateFiles(Path.Combine(root, "src"), "*.razor", SearchOption.AllDirectories)
                        .Where(f => f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") is false)
                        .Where(f => f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") is false);
    }

    /// <summary>
    /// Walks up from the test assembly to the directory that owns <c>.template.config/template.json</c>, the same
    /// anchor <c>SourceNameLeakTests</c> and <c>TemplateConfigurationTests</c> use.
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
