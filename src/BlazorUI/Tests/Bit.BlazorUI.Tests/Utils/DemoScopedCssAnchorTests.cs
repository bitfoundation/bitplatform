using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils;

/// <summary>
/// Pins that every class a demo stylesheet styles through an unanchored <c>::deep</c> is used under a plain HTML
/// element of its own .razor file. <c>::deep .foo</c> compiles to <c>[b-scope] .foo</c>, and the scope attribute
/// lands only on the plain elements written in the .razor that owns the stylesheet - never on what a component
/// renders. A class handed only to components (<c>Class</c>, <c>Classes</c>, a template) therefore matches nothing:
/// the preview looks unstyled while the sample, which carries a global &lt;style&gt;, still looks right, and nothing
/// else fails.
/// <para>
/// The reading is a heuristic. A rule anchored by a selector in front of its <c>::deep</c> (<c>.wrapper ::deep .x</c>)
/// is left alone, and so are the library's own <c>bit-*</c> classes. A class is looked for in the attributes that can
/// carry one - any attribute named after a class, and every C# expression - and a use it cannot read (a class set
/// from the @code block or the code-behind) is not checked. A use the reading gets wrong goes in
/// <see cref="Exemptions"/>, with the reason beside it.
/// </para>
/// </summary>
[TestClass]
public partial class DemoScopedCssAnchorTests
{
    /// <summary>
    /// The uses the heuristic misreads, as "path/of/Page.razor .class" relative to the demo client project, each with
    /// a comment saying why it is anchored after all.
    /// </summary>
    private static readonly HashSet<string> Exemptions = new(StringComparer.Ordinal)
    {
    };

    // Razor's <text> is a markup marker, not an element: nothing renders it, so nothing carries the scope attribute.
    private static readonly HashSet<string> NonElements = new(StringComparer.Ordinal) { "text" };

    private static readonly HashSet<string> VoidElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "source", "track", "wbr"
    };

    [TestMethod]
    public void EveryDeepClassOfADemoStylesheetShouldBeUsedUnderAPlainElementOfItsOwnRazorFile()
    {
        var root = SourceFiles.GetDirectory("Demo", "Client", "Bit.BlazorUI.Demo.Client.Core");
        var stylesheets = Directory.EnumerateFiles(root, "*.razor.scss", SearchOption.AllDirectories)
                                   .Where(path => IsBuildOutput(root, path) is false)
                                   .ToArray();

        Assert.IsTrue(stylesheets.Length > 0, "No isolated stylesheet was found in the demo client.");

        var checkedPages = 0;
        var offenders = new List<string>();

        foreach (var stylesheet in stylesheets)
        {
            var razor = stylesheet[..^".scss".Length];
            if (File.Exists(razor) is false) continue;

            var classes = GetUnanchoredDeepClasses(SourceFiles.ReadFullPath(stylesheet));
            if (classes.Count == 0) continue;

            checkedPages++;
            var page = Path.GetRelativePath(root, razor).Replace('\\', '/');

            foreach (var (cssClass, tag, line) in FindUnanchoredUses(SourceFiles.ReadFullPath(razor), classes))
            {
                if (Exemptions.Contains($"{page} .{cssClass}")) continue;

                offenders.Add($"{page}:{line} .{cssClass} on <{tag}>");
            }
        }

        Assert.IsTrue(checkedPages > 0, "No demo stylesheet styles a class through ::deep; the reading of the stylesheets is broken.");

        CollectionAssert.AreEqual(Array.Empty<string>(), offenders.ToArray(),
            "These classes are styled through ::deep but used with no plain HTML element of the page above them, so the " +
            "page's scope attribute never reaches them and the rule matches nothing. Wrap the markup in a plain element " +
            $"(a <div>) inside the DemoExample:{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    [TestMethod]
    [DataRow("::deep .custom-root { color: red; }")]
    [DataRow("::deep {\n    .custom-root { color: red; }\n}")]
    [DataRow("::deep {\n    .other {\n        &.custom-root { color: red; }\n    }\n}")]
    [DataRow("@media (max-width: 600px) {\n    ::deep .custom-root { color: red; }\n}")]
    [DataRow("> ::deep .custom-root { color: red; }")]
    public void AnUnanchoredDeepRuleShouldBeRead(string stylesheet)
    {
        CollectionAssert.Contains(GetUnanchoredDeepClasses(stylesheet).ToArray(), "custom-root");
    }

    [TestMethod]
    [DataRow(".wrapper ::deep .custom-root { color: red; }")]
    [DataRow(".wrapper {\n    ::deep .custom-root { color: red; }\n}")]
    [DataRow("::deep .bit-btn { color: red; }")]
    [DataRow(".custom-root { color: red; }")]
    [DataRow("// ::deep .custom-root\n.x { color: red; }")]
    public void AnAnchoredOrPlainRuleShouldNotBeRead(string stylesheet)
    {
        Assert.AreEqual(0, GetUnanchoredDeepClasses(stylesheet).Count, stylesheet);
    }

    [TestMethod]
    [DataRow("<DemoExample Title=\"Style\">\n    <BitPersona Class=\"custom-root\" />\n</DemoExample>")]
    [DataRow("<DemoExample Title=\"Style\">\n    <BitCollapse Classes=\"@(new() { Root = \"custom-root\", Wrapper = \"x\" })\" />\n</DemoExample>")]
    [DataRow("<DemoExample Title=\"Style\">\n    <BitSwiper>\n        <BitSwiperItem><div class=\"custom-root\"></div></BitSwiperItem>\n    </BitSwiper>\n</DemoExample>")]
    [DataRow("<DemoExample Title=\"Style\">\n    <BitPersona>\n        <PrimaryTextTemplate><text><i class=\"custom-root\"></i></text></PrimaryTextTemplate>\n    </BitPersona>\n</DemoExample>")]
    [DataRow("<div>\n</div>\n<DemoExample Title=\"Style\">\n    <BitPersona Class=\"custom-root\" />\n</DemoExample>")]
    public void AUseWithNoPlainElementAboveItShouldBeFound(string razor)
    {
        var uses = FindUnanchoredUses(razor, new HashSet<string> { "custom-root" }).ToArray();

        Assert.AreEqual(1, uses.Length, razor);
        Assert.AreEqual("custom-root", uses[0].Class);
    }

    [TestMethod]
    [DataRow("<DemoExample Title=\"Style\">\n    <div>\n        <BitPersona Class=\"custom-root\" />\n    </div>\n</DemoExample>")]
    [DataRow("<DemoExample Title=\"Style\">\n    <BitSwiper>\n        <BitSwiperItem><div><span class=\"custom-root\"></span></div></BitSwiperItem>\n    </BitSwiper>\n</DemoExample>")]
    [DataRow("<DemoPage Description=\"Shows the custom-root of a persona\">\n</DemoPage>")]
    [DataRow("@* <BitPersona Class=\"custom-root\" /> *@\n<DemoExample Title=\"Style\" />")]
    [DataRow("<DemoExample Title=\"Style\" />\n@code {\n    private RenderFragment x = @<BitPersona Class=\"custom-root\" />;\n}")]
    [DataRow("<DemoExample Title=\"Style\">\n    <BitPersona Class=\"custom-root-other\" />\n</DemoExample>")]
    public void AnAnchoredOrUnrelatedUseShouldNotBeFound(string razor)
    {
        Assert.AreEqual(0, FindUnanchoredUses(razor, new HashSet<string> { "custom-root" }).Count(), razor);
    }

    // The classes of the rules under a ::deep with no selector in front of it: the one the ::deep itself names and
    // every one nested in it. An at-rule around it (@media) anchors nothing; a selector around it, or in front of it in
    // the same compound selector, is the anchor the scope attribute lands on, so the rule is left alone.
    private static HashSet<string> GetUnanchoredDeepClasses(string stylesheet)
    {
        var classes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var rule in SourceFiles.GetScssRules(stylesheet))
        {
            var chain = rule.Ancestors.Append(rule.Header).ToArray();
            var deep = Array.FindIndex(chain, header => header.Contains("::deep", StringComparison.Ordinal));
            if (deep < 0) continue;

            if (chain.Take(deep).Any(IsSelector)) continue;

            var unanchored = chain[deep].Split(',')
                                        .Select(part => part.Split("::deep", 2))
                                        .Where(halves => halves.Length == 2 && IsEmptyAnchor(halves[0]))
                                        .ToArray();
            if (unanchored.Length == 0) continue;

            // The ::deep rule itself names what follows the ::deep; a rule nested in it names its whole selector.
            string[] selectors = deep == chain.Length - 1 ? [.. unanchored.Select(halves => halves[1])]
                               : IsSelector(rule.Header) ? [rule.Header]
                               : [];

            foreach (var selector in selectors)
            {
                foreach (Match match in ClassSelector().Matches(selector))
                {
                    var name = match.Groups["name"].Value;
                    if (name.StartsWith("bit-", StringComparison.Ordinal) is false) classes.Add(name);
                }
            }
        }

        return classes;

        static bool IsSelector(string header) => header.StartsWith('@') is false;

        static bool IsEmptyAnchor(string prefix) => prefix.Trim().TrimStart('&').Trim().TrimEnd('>').Trim().Length == 0;
    }

    // Every element or component of the markup carrying one of the classes with no plain HTML element of the file
    // above it. The element carrying the class does not anchor itself: [b-scope] .foo matches a descendant only.
    private static IEnumerable<(string Class, string Tag, int Line)> FindUnanchoredUses(string razor, IReadOnlySet<string> classes)
    {
        // Keep the line breaks of what is dropped, so every tag stays on the line it is on.
        var markup = RazorComment().Replace(razor, comment => new string('\n', comment.Value.Count(c => c == '\n')));
        var code = CodeBlock().Match(markup);
        if (code.Success) markup = markup[..code.Index];

        var stack = new List<string>();

        foreach (Match tag in Tag().Matches(markup))
        {
            var name = tag.Groups["name"].Value;

            if (tag.Groups["close"].Length > 0)
            {
                var open = stack.LastIndexOf(name);
                if (open >= 0) stack.RemoveRange(open, stack.Count - open);
                continue;
            }

            var values = GetClassBearingValues(tag.Groups["attributes"].Value);

            if (values.Length > 0 && stack.Any(IsPlainElement) is false)
            {
                foreach (var cssClass in classes)
                {
                    if (Regex.IsMatch(values, $@"(?<![\w-]){Regex.Escape(cssClass)}(?![\w-])"))
                    {
                        yield return (cssClass, name, GetLine(markup, tag.Index));
                    }
                }
            }

            if (tag.Groups["self"].Length == 0 && VoidElements.Contains(name) is false)
            {
                stack.Add(name);
            }
        }

        static bool IsPlainElement(string name) => char.IsLower(name[0]) && NonElements.Contains(name) is false;
    }

    // The values of the attributes that can hand an element or a component a class: one named after a class (class,
    // Class, Classes, IconClass, ...) and any C# expression. A plain-text value of another attribute - a page's
    // Description - is prose, and a word of it that happens to be a class name is not a use. An @(...) value is read
    // to the parenthesis closing it, since the strings inside it are quoted too.
    private static string GetClassBearingValues(string attributes)
    {
        var values = new List<string>();
        var i = 0;

        while (i < attributes.Length)
        {
            var attribute = AttributeStart().Match(attributes, i);
            if (attribute.Success is false)
            {
                i++;
                continue;
            }

            var start = attribute.Index + attribute.Length;
            var end = start;

            if (string.CompareOrdinal(attributes, start, "@(", 0, 2) == 0)
            {
                var depth = 0;
                for (end = start + 1; end < attributes.Length; end++)
                {
                    if (attributes[end] == '(') depth++;
                    else if (attributes[end] == ')' && --depth == 0) break;
                }
            }

            end = attributes.IndexOf('"', Math.Min(end, attributes.Length));
            if (end < 0) end = attributes.Length;

            var value = attributes[start..end];
            if (attribute.Groups["name"].Value.Contains("class", StringComparison.OrdinalIgnoreCase) || value.TrimStart().StartsWith('@'))
            {
                values.Add(value);
            }

            i = end + 1;
        }

        return string.Join(' ', values);
    }

    private static int GetLine(string text, int index) => text.AsSpan(0, index).Count('\n') + 1;

    private static bool IsBuildOutput(string root, string path)
    {
        var first = Path.GetRelativePath(root, path).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];

        return first is "bin" or "obj";
    }

    [GeneratedRegex(@"(?<![\w-])\.(?<name>-?[A-Za-z_][\w-]*)")]
    private static partial Regex ClassSelector();

    [GeneratedRegex(@"@\*.*?\*@", RegexOptions.Singleline)]
    private static partial Regex RazorComment();

    [GeneratedRegex(@"^@(code|functions)\s*\{", RegexOptions.Multiline)]
    private static partial Regex CodeBlock();

    [GeneratedRegex(@"<(?<close>/?)(?<name>[A-Za-z][\w.:-]*)(?<attributes>(?:[^>""']|""[^""]*""|'[^']*')*?)(?<self>/?)>")]
    private static partial Regex Tag();

    [GeneratedRegex(@"\G\s*(?<name>[\w@:.-]+)\s*=\s*""")]
    private static partial Regex AttributeStart();
}
