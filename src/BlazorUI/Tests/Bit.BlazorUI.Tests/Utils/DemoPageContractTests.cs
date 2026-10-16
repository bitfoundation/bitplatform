using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Bit.BlazorUI.Demo.Client.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils;

/// <summary>
/// Pins the rules of the demo pages that src/BlazorUI/CLAUDE.md writes down and nothing else checks: the order of
/// the look-and-feel sections, the numbering of the examples and of their sample fields, the links to an example,
/// the aliases a page shares with its nav item, the link from a parameter typed with a library-wide enum to the
/// shared table of it, and the Razor comment that must never sit inside a tag. Every rule reads the source tree,
/// the way <see cref="DemoSharedEnumsTests"/> and <see cref="DemoScopedCssAnchorTests"/> do, and every failure names
/// the file and the section, so it can be fixed from the message alone.
/// <para>
/// The pages of Bit.BlazorUI.Legacy (Pages/Components/Legacy) are left out: they are kept as they were released.
/// </para>
/// </summary>
[TestClass]
public partial class DemoPageContractTests
{
    /// <summary>The five look-and-feel sections, in the order they close a demo page.</summary>
    private static readonly string[] LookAndFeelSections = ["Color", "External Icons", "Size", "Style & Class", "RTL"];

    /// <summary>
    /// The folders, relative to the demo client, whose examples are not numbered exampleN, each with the reason.
    /// </summary>
    private static readonly HashSet<string> NumberingExemptions = new(StringComparer.Ordinal)
    {
        // BitChart is no multi-API component whose tabs repeat one run of sections in another API: each of its tabs
        // is a topic of its own (a chart type or a feature), with examples no other tab has. Its Ids are named after
        // the tab and the example (bar-grouped, area-stacked) so every one of them is unique on the whole page - a
        // deep link names the chart it shows, and stays put when a tab gains an example - and its samples are
        // declared beside the tab's code under the same names (groupedRazorCode).
        "Pages/Components/Extras/Chart",
    };

    /// <summary>
    /// The tables every page of a component inheriting BitComponentBase shows under its API section (DemoPage's
    /// <c>_componentBaseSubEnums</c>), by the enum they document: a page of a component that does not inherit it
    /// (DemoPage's <c>_notInheritedComponents</c>) adds the one it links to its own list, through the factory named.
    /// </summary>
    private static readonly Dictionary<string, (string Id, string Factory)> ComponentBaseTables = new(StringComparer.Ordinal)
    {
        ["BitDir"] = ("component-dir", "DemoPage.ComponentDirEnum"),
        ["BitVisibility"] = ("component-visibility", "DemoPage.ComponentVisibilityEnum"),
    };

    [TestMethod]
    public void TheLookAndFeelSectionsShouldCloseEveryPageInTheirOrder()
    {
        var files = ExampleFiles().ToArray();

        Assert.IsTrue(files.Length > 50, $"Only {files.Length} demo files with examples were found.");

        var offenders = new List<string>();

        foreach (var file in files)
        {
            var titles = ReadExamples(file).Select(e => e.Title).ToArray();
            var present = titles.Where(LookAndFeelSections.Contains).ToArray();
            if (present.Length == 0) continue;

            var expected = LookAndFeelSections.Where(present.Contains).ToArray();
            var tail = titles[^Math.Min(titles.Length, present.Length)..];

            if (present.SequenceEqual(expected) && tail.SequenceEqual(expected)) continue;

            offenders.Add($"{Relative(file)}: the sections run {string.Join(" | ", titles)}; the last ones have to be {string.Join(" | ", expected)}, after every section of the component's own.");
        }

        Fail(offenders,
            $"The look-and-feel sections close a demo page, in this order: {string.Join(", ", LookAndFeelSections)}. " +
            "A new section goes after the last component-specific one, before Color - and renumbering follows (see the numbering test).");
    }

    [TestMethod]
    public void EveryExampleShouldBeNumberedFromOneInTheOrderItRenders()
    {
        var offenders = new List<string>();
        var files = 0;

        foreach (var file in ExampleFiles())
        {
            if (IsNumberingExempt(file)) continue;

            files++;
            var page = Relative(file);

            foreach (var (example, n) in ReadExamples(file).Select((e, i) => (e, i + 1)))
            {
                var section = $"{page}:{example.Line} \"{example.Title}\"";

                if (example.Id != $"example{n}")
                {
                    offenders.Add($"{section} is section {n} but has Id=\"{example.Id}\"; it has to be Id=\"example{n}\".");
                }

                foreach (var (attribute, kind) in SampleAttributes)
                {
                    if (example.Attributes.TryGetValue(attribute, out var value) is false) continue;

                    if (value != $"@example{n}{kind}")
                    {
                        offenders.Add($"{section} is section {n} but has {attribute}=\"{value}\"; it has to be {attribute}=\"@example{n}{kind}\".");
                    }
                }
            }
        }

        Assert.IsTrue(files > 50, $"Only {files} demo files with examples were numbered.");

        Fail(offenders,
            "The examples of a demo file (of each tab of a multi-API component) are numbered from 1 with no gap in the " +
            "order they render, and the Id and the sample fields of one share its number. Reordering or inserting a section " +
            "renumbers every later one, in the .razor and in the .razor.samples.cs, and every Href=\"#exampleN\" to them.");
    }

    [TestMethod]
    public void EverySampleFieldShouldBeDeclaredOnceInTheOrderOfTheExamples()
    {
        var offenders = new List<string>();

        foreach (var file in ExampleFiles())
        {
            if (IsNumberingExempt(file)) continue;

            var page = Relative(file);
            var examples = ReadExamples(file);
            var referenced = examples.SelectMany(e => SampleAttributes.Select(a => e.Attributes.GetValueOrDefault(a.Attribute)))
                                     .Where(v => v is not null && v.StartsWith('@'))
                                     .Select(v => v![1..])
                                     .ToHashSet(StringComparer.Ordinal);

            var declared = new List<(string Field, int Number, string Source)>();

            foreach (var source in PartialSources(file))
            {
                var fields = SampleFieldDeclaration().Matches(BlankCSharpLiterals(SourceFiles.ReadFullPath(source)))
                                                     .Select(m => (Field: m.Groups["field"].Value, Number: int.Parse(m.Groups["n"].Value)))
                                                     .ToArray();

                if (fields.Select(f => f.Number).SequenceEqual(fields.Select(f => f.Number).Order()) is false)
                {
                    offenders.Add($"{Relative(source)} declares its sample fields out of the order of the examples: {string.Join(", ", fields.Select(f => f.Field))}.");
                }

                declared.AddRange(fields.Select(f => (f.Field, f.Number, Relative(source))));
            }

            foreach (var duplicate in declared.GroupBy(d => d.Field).Where(g => g.Count() > 1))
            {
                offenders.Add($"{page}: {duplicate.Key} is declared more than once ({string.Join(", ", duplicate.Select(d => d.Source))}).");
            }

            foreach (var field in referenced.Where(r => declared.Any(d => d.Field == r) is false).Order(StringComparer.Ordinal))
            {
                offenders.Add($"{page}: an example reads {field}, which none of {string.Join(", ", PartialSources(file).Select(Relative))} declares.");
            }

            foreach (var (field, _, source) in declared.Where(d => referenced.Contains(d.Field) is false))
            {
                offenders.Add($"{source} declares {field}, which no example of {page} reads - left behind by a renumbering, or the example lost its attribute.");
            }
        }

        Fail(offenders,
            "Every exampleNRazorCode / exampleNCsharpCode / exampleNCodeFiles an example reads is declared once in the page's " +
            "partial class, in the order of the examples, and no other is left over.");
    }

    [TestMethod]
    public void EveryLinkToAnExampleShouldPointAtOneOnItsPage()
    {
        var offenders = new List<string>();
        var links = 0;

        foreach (var file in ExampleFiles())
        {
            if (IsNumberingExempt(file)) continue;

            var ids = ReadExamples(file).Select(e => e.Id).ToHashSet(StringComparer.Ordinal);

            // The markup, and the code-behind it renders strings from; never the samples, which are text to copy.
            string[] sources = [file, .. new[] { file + ".cs" }.Where(File.Exists)];

            foreach (var source in sources)
            {
                var text = source == file ? StripRazorComments(SourceFiles.ReadFullPath(source)) : SourceFiles.ReadFullPath(source);

                foreach (Match link in ExampleLink().Matches(text))
                {
                    links++;
                    var id = link.Groups["id"].Value;

                    if (ids.Contains(id)) continue;

                    offenders.Add($"{Relative(source)}:{GetLine(text, link.Index)} links #{id}, but {Relative(file)} renders no example with Id=\"{id}\" (its last is example{ids.Count}).");
                }
            }
        }

        Assert.IsTrue(links > 20, $"Only {links} links to an example were found; the reading of the pages is broken.");

        Fail(offenders, "A link to #exampleN points at an example of the file it is in (of its own tab, for a multi-API component); renumbering a section renumbers the links to it.");
    }

    [TestMethod]
    public void EveryPageShouldBeCalledWhatItsNavItemIsAlsoCalled()
    {
        var navItems = ReadNavItems();

        Assert.IsTrue(navItems.Count > 80, $"Only {navItems.Count} nav items with a url were read off MainLayout.razor.NavItems.cs.");

        var offenders = new List<string>();
        var pages = DemoPages().ToArray();

        Assert.IsTrue(pages.Length > 80, $"Only {pages.Length} demo pages were found.");

        foreach (var page in pages)
        {
            var razor = StripRazorComments(SourceFiles.ReadFullPath(page.Path));
            var routes = PageRoute().Matches(razor).Select(m => m.Groups["route"].Value).ToArray();
            var items = navItems.Where(item => item.Urls.Intersect(routes, StringComparer.OrdinalIgnoreCase).Any()).ToArray();

            if (items.Length != 1)
            {
                offenders.Add($"{Relative(page.Path)} (routes {string.Join(", ", routes)}) is reached through {items.Length} nav items rather than one: {string.Join(", ", items.Select(i => i.Text))}.");
                continue;
            }

            var item = items[0];
            var expected = item.Description is null ? [] : item.Description.Split(',').Select(n => n.Trim()).ToArray();

            var attribute = SecondaryNamesAttribute().Match(page.Tag);
            string[] actual = [];

            if (attribute.Success)
            {
                var list = SecondaryNamesList().Match(attribute.Value);

                if (list.Success is false)
                {
                    offenders.Add($"{Relative(page.Path)} writes {attribute.Value}; write SecondaryNames=\"@([\"A\", \"B\"])\".");
                    continue;
                }

                actual = [.. QuotedString().Matches(list.Groups["names"].Value).Select(m => m.Groups["value"].Value)];
            }

            if (actual.SequenceEqual(expected, StringComparer.Ordinal)) continue;

            var navSays = item.Description is null ? "has no Description, so the page has no SecondaryNames" : $"has Description = \"{item.Description}\", so the page has SecondaryNames=\"@([{string.Join(", ", expected.Select(n => $"\"{n}\""))}])\"";

            offenders.Add($"{Relative(page.Path)} has SecondaryNames [{string.Join(", ", actual)}], but its nav item \"{item.Text}\" {navSays}.");
        }

        Fail(offenders,
            "The Description of a component's nav item (Shared/MainLayout.razor.NavItems.cs) is the one list of what it is also " +
            "called; its page's SecondaryNames are the same names, in the same order and spelling. A new alias goes into the nav item first.");
    }

    [TestMethod]
    public void EveryParameterTypedWithASharedEnumShouldLinkItsTable()
    {
        var shared = typeof(DemoSharedEnums).GetMethods(BindingFlags.Public | BindingFlags.Static)
                                            .Where(m => m.ReturnType == typeof(ComponentSubEnum) && m.Name != nameof(DemoSharedEnums.Only))
                                            .Select(m => (ComponentSubEnum)m.Invoke(null, [.. m.GetParameters().Select(p => p.DefaultValue)])!)
                                            .ToDictionary(t => t.Name!, t => (Id: t.Id!, Factory: $"DemoSharedEnums.{t.Name}"), StringComparer.Ordinal);

        Assert.IsTrue(shared.Count > 20, $"Only {shared.Count} shared tables were read off DemoSharedEnums.");

        var demoPage = SourceFiles.Read("Demo", "Client", "Bit.BlazorUI.Demo.Client.Core", "Components", "DemoPage.razor.cs");

        foreach (var (type, (id, factory)) in ComponentBaseTables)
        {
            Assert.IsTrue(Regex.IsMatch(demoPage, $@"Id\s*=\s*""{Regex.Escape(id)}""\s*,\s*Name\s*=\s*""{type}"""),
                $"DemoPage.razor.cs no longer anchors its {type} table under {id}; update {nameof(ComponentBaseTables)}.");
            Assert.IsTrue(demoPage.Contains($"public static ComponentSubEnum {factory.Split('.')[1]}()", StringComparison.Ordinal),
                $"DemoPage.razor.cs no longer builds its {type} table in {factory}(); update {nameof(ComponentBaseTables)}.");

            shared[type] = (id, factory);
        }

        var notInherited = NotInheritedComponents().Match(demoPage);
        Assert.IsTrue(notInherited.Success, "DemoPage.razor.cs no longer lists its _notInheritedComponents.");
        var notInheritedNames = QuotedString().Matches(notInherited.Groups["names"].Value).Select(m => m.Groups["value"].Value).ToHashSet(StringComparer.Ordinal);

        var offenders = new List<string>();
        var rows = 0;

        foreach (var page in DemoPages())
        {
            var sources = PageCodeBehinds(page).ToArray();
            var code = string.Concat(sources.Select(SourceFiles.ReadFullPath));
            var name = PageName().Match(page.Tag).Groups["name"].Value;
            var hasSubEnums = SubEnumsAttribute().IsMatch(page.Tag);

            foreach (var source in sources)
            {
                var text = SourceFiles.ReadFullPath(source);

                foreach (var row in ReadRows(text))
                {
                    var type = TypeAssignment().Match(row.Text);
                    if (type.Success is false || shared.TryGetValue(type.Groups["type"].Value, out var table) is false) continue;

                    rows++;
                    var enumName = type.Groups["type"].Value;
                    var parameter = NameAssignment().Match(row.Text).Groups["value"].Value;
                    var at = $"{Relative(source)}:{GetLine(text, row.Index)} {parameter} ({type.Groups["type"].Value}{type.Groups["nullable"].Value})";

                    var href = HrefAssignment().Match(row.Text);
                    if (href.Success is false || href.Groups["value"].Value != $"#{table.Id}" || LinkTypeLink().IsMatch(row.Text) is false)
                    {
                        offenders.Add($"{at} has to link the {enumName} table: LinkType = LinkType.Link, Href = \"#{table.Id}\"{(href.Success ? $" (it has Href = \"{href.Groups["value"].Value}\")" : "")}.");
                    }

                    var ownTable = code.Contains($"{table.Factory}(", StringComparison.Ordinal)
                                || Regex.IsMatch(code, $@"\bId\s*=\s*""{Regex.Escape(table.Id)}""");
                    var baseTable = ComponentBaseTables.ContainsKey(enumName) && notInheritedNames.Contains(name) is false;

                    if (baseTable || (ownTable && hasSubEnums)) continue;

                    offenders.Add($"{at} links #{table.Id}, but {Relative(page.Path)} renders no table under that id: add {table.Factory}() to its componentSubEnums" +
                                  (hasSubEnums ? "." : " and pass that list to the DemoPage as SubEnums=\"componentSubEnums\"."));
                }
            }
        }

        Assert.IsTrue(rows > 100, $"Only {rows} parameter rows typed with a shared enum were read; the reading of the pages is broken.");

        Fail(offenders,
            "A parameter row whose Type is an enum with a shared table (DemoSharedEnums, or BitComponentBase's BitDir and BitVisibility), " +
            "nullable or not, links that table, and the page renders the table it links to.");
    }

    [TestMethod]
    public void NoRazorCommentShouldSitInsideATag()
    {
        string[][] roots = [["Bit.BlazorUI"], ["Bit.BlazorUI.Extras"], ["Demo"]];

        var files = roots.SelectMany(r => Directory.EnumerateFiles(SourceFiles.GetDirectory(r), "*.razor", SearchOption.AllDirectories))
                         .Where(f => IsExcluded(f) is false)
                         .ToArray();

        Assert.IsTrue(files.Length > 300, $"Only {files.Length} .razor files were found.");

        var offenders = new List<string>();

        foreach (var file in files)
        {
            var razor = SourceFiles.ReadFullPath(file);

            foreach (var (tag, comment) in FindCommentsInsideTags(razor))
            {
                offenders.Add($"{Relative(file)}:{GetLine(razor, comment)} a @* comment *@ inside <{tag}>");
            }
        }

        Fail(offenders,
            "A Razor comment inside a tag, between its attributes, compiles but makes the Razor compiler misread the attributes " +
            "around it, and the page throws at runtime. Move it to the comment block right above the element's opening tag.");
    }

    [TestMethod]
    [DataRow("<div class=\"x\"\n     @* why *@\n     aria-label=\"y\">\n</div>", "div")]
    [DataRow("<BitButton @* why *@ OnClick=\"Go\" />", "BitButton")]
    [DataRow("<BitButton Class=\"@(a ? \"x\" : \"y\")\" @* why *@ />", "BitButton")]
    [DataRow("<BitButton OnClick=\"@(() => { Go(); })\"\n           @* why *@ />", "BitButton")]
    [DataRow("@* a comment *@\n<div>\n    <span @* why *@ class=\"x\"></span>\n</div>", "span")]
    public void ACommentInsideATagShouldBeFound(string razor, string tag)
    {
        var found = FindCommentsInsideTags(razor).ToArray();

        Assert.AreEqual(1, found.Length, razor);
        Assert.AreEqual(tag, found[0].Tag);
    }

    [TestMethod]
    [DataRow("@* Why the chip is named through aria-label. *@\n<div role=\"listitem\"\n     aria-label=\"@TagName(tag)\">\n</div>")]
    [DataRow("<div>@* between children *@</div>")]
    [DataRow("@* <div @* nested-looking *@ *@")]
    [DataRow("<div title=\"@* not a comment in a value *@\"></div>")]
    [DataRow("@code {\n    private List<string> items = [];\n    private bool Less(int a, int b) => a < b; @* after *@\n}")]
    [DataRow("<p>if a <b then</p> @* fine *@")]
    public void ACommentOutsideATagShouldNotBeFound(string razor)
    {
        Assert.AreEqual(0, FindCommentsInsideTags(razor).Count(), razor);
    }

    [TestMethod]
    public void BlankCSharpLiteralsShouldHideTheBracesOfStringsCharsAndComments()
    {
        var source = """
            var a = new() { Name = "x {y}", Type = "BitColor?" };
            var b = @"{""}"; var c = '{'; var d = '\''; // {
            var e = $"{(f ? "}" : "{")} {{"; /* { */ var g = new() { };
            var h = \"\"\"{"}\"\"\";
            """.Replace("\\\"", "\"");

        var blanked = BlankCSharpLiterals(source);

        Assert.AreEqual(source.Length, blanked.Length);
        Assert.AreEqual(source.Count(c => c == '\n'), blanked.Count(c => c == '\n'));
        Assert.AreEqual(2, blanked.Count(c => c == '{'), blanked);
        Assert.AreEqual(2, blanked.Count(c => c == '}'), blanked);
    }

    // The attributes of an example that name a sample field, with the suffix of the field.
    private static readonly (string Attribute, string Kind)[] SampleAttributes = [("RazorCode", "RazorCode"), ("CsharpCode", "CsharpCode"), ("CodeFiles", "CodeFiles")];

    private sealed record Example(string Title, string Id, int Line, IReadOnlyDictionary<string, string> Attributes);

    private sealed record DemoPageFile(string Path, string Tag);

    private sealed record NavItem(string Text, string[] Urls, string? Description);

    // Every .razor of the demo client's pages that renders a DemoExample.
    private static IEnumerable<string> ExampleFiles()
    {
        var pages = SourceFiles.GetDirectory("Demo", "Client", "Bit.BlazorUI.Demo.Client.Core", "Pages");

        return Directory.EnumerateFiles(pages, "*.razor", SearchOption.AllDirectories)
                        .Where(f => IsExcluded(f) is false)
                        .Where(f => SourceFiles.ReadFullPath(f).Contains("<DemoExample", StringComparison.Ordinal))
                        .Order(StringComparer.Ordinal);
    }

    // Every .razor of the demo client's pages that renders a DemoPage, with the opening tag of the DemoPage.
    private static IEnumerable<DemoPageFile> DemoPages()
    {
        var pages = SourceFiles.GetDirectory("Demo", "Client", "Bit.BlazorUI.Demo.Client.Core", "Pages");

        foreach (var file in Directory.EnumerateFiles(pages, "*.razor", SearchOption.AllDirectories).Where(f => IsExcluded(f) is false).Order(StringComparer.Ordinal))
        {
            var razor = StripRazorComments(SourceFiles.ReadFullPath(file));
            var start = razor.IndexOf("<DemoPage", StringComparison.Ordinal);
            if (start < 0) continue;

            Assert.IsTrue(TryReadTag(razor, start, out _, out var end, null), $"The <DemoPage> tag of {Relative(file)} could not be read.");

            yield return new(file, razor[start..(end + 1)]);
        }
    }

    // The code-behinds of a page: its own and those of the tabs it renders (_BitFooItemDemo), never the samples.
    private static IEnumerable<string> PageCodeBehinds(DemoPageFile page)
    {
        var folder = Path.GetDirectoryName(page.Path)!;
        var razor = SourceFiles.ReadFullPath(page.Path);

        string[] razors = [page.Path, .. TabTag().Matches(razor).Select(m => Path.Combine(folder, m.Groups["tab"].Value + ".razor")).Distinct().Where(File.Exists)];

        return razors.Select(r => r + ".cs").Where(File.Exists);
    }

    // The files of the partial class a .razor file is compiled into: its code-behind and samples (BitFooDemo.razor.cs,
    // BitFooDemo.razor.samples.cs, ...).
    private static IEnumerable<string> PartialSources(string razor)
    {
        return Directory.EnumerateFiles(Path.GetDirectoryName(razor)!, Path.GetFileName(razor) + "*.cs")
                        .Where(f => Path.GetFileName(f) == Path.GetFileName(razor) + ".cs" || Path.GetFileName(f).StartsWith(Path.GetFileName(razor) + ".", StringComparison.Ordinal))
                        .Order(StringComparer.Ordinal);
    }

    // The DemoExample tags of a .razor file, in the order they render; the commented-out ones are left out.
    private static List<Example> ReadExamples(string file)
    {
        var razor = StripRazorComments(SourceFiles.ReadFullPath(file));
        var examples = new List<Example>();

        foreach (Match start in DemoExampleStart().Matches(razor))
        {
            Assert.IsTrue(TryReadTag(razor, start.Index, out _, out var end, null), $"{Relative(file)}:{GetLine(razor, start.Index)} the <DemoExample> tag could not be read.");

            var attributes = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (Match attribute in TagAttribute().Matches(razor[start.Index..end]))
            {
                attributes.TryAdd(attribute.Groups["name"].Value, attribute.Groups["value"].Value);
            }

            examples.Add(new(WebUtility.HtmlDecode(attributes.GetValueOrDefault("Title") ?? ""),
                             attributes.GetValueOrDefault("Id") ?? "(none)",
                             GetLine(razor, start.Index),
                             attributes));
        }

        return examples;
    }

    // The leaf items of the nav, each with its urls (Url and AdditionalUrls) and its Description.
    private static List<NavItem> ReadNavItems()
    {
        var source = SourceFiles.Read("Demo", "Client", "Bit.BlazorUI.Demo.Client.Core", "Shared", "MainLayout.razor.NavItems.cs");
        var items = new List<NavItem>();

        foreach (var row in ReadRows(source))
        {
            var url = UrlAssignment().Match(row.Text);
            if (url.Success is false) continue;

            var additional = AdditionalUrlsAssignment().Match(row.Text);
            var description = DescriptionAssignment().Match(row.Text);

            items.Add(new(TextAssignment().Match(row.Text).Groups["value"].Value,
                          [url.Groups["value"].Value, .. additional.Success ? QuotedString().Matches(additional.Groups["urls"].Value).Select(m => m.Groups["value"].Value) : []],
                          description.Success ? description.Groups["value"].Value : null));
        }

        return items;
    }

    // Every innermost object initializer of a C# source - a parameter row, a nav item - with where it starts. The
    // braces are found with the string and character literals and the comments blanked out, since a description
    // may well hold a brace ("{0} of {1}"); the text handed back is the source's own.
    private static IEnumerable<(string Text, int Index)> ReadRows(string source)
    {
        foreach (Match row in InnermostBraces().Matches(BlankCSharpLiterals(source)))
        {
            yield return (source.Substring(row.Index, row.Length), row.Index);
        }
    }

    /// <summary>
    /// A C# source with the content of every string and character literal and of every comment replaced by spaces
    /// (line breaks kept, so every index and line stays where it was), so that a brace or a quote inside one is not
    /// read as code. Regular, verbatim, interpolated and raw strings are all read; the code in an interpolation hole is
    /// blanked with the string.
    /// </summary>
    private static string BlankCSharpLiterals(string source)
    {
        var chars = source.ToCharArray();

        ScanCode(chars, 0, inHole: false);

        return new string(chars);
    }

    // Reads code from i. In an interpolation hole it stops at the brace closing the hole and returns its index.
    private static int ScanCode(char[] c, int i, bool inHole)
    {
        var depth = 0;

        while (i < c.Length)
        {
            var ch = c[i];

            if (ch == '/' && At(c, i + 1) == '/')
            {
                var end = i;
                while (end < c.Length && c[end] != '\n') end++;
                Blank(c, i, end);
                i = end;
            }
            else if (ch == '/' && At(c, i + 1) == '*')
            {
                var end = IndexOf(c, "*/", i + 2);
                end = end < 0 ? c.Length : end + 2;
                Blank(c, i, end);
                i = end;
            }
            else if (ch == '\'')
            {
                // A character literal, escapes ('\'', 'A') included, never runs past its line.
                var end = i + 1;
                while (end < c.Length && c[end] is not '\'' and not '\n')
                {
                    if (c[end] == '\\') end++;
                    end++;
                }

                if (At(c, end) == '\'')
                {
                    Blank(c, i + 1, end);
                    i = end + 1;
                }
                else
                {
                    i++;
                }
            }
            else if (TryReadStringStart(c, i, out var dollars, out var verbatim, out var quotes, out var contentStart))
            {
                i = ScanString(c, contentStart, dollars, verbatim, quotes);
            }
            else
            {
                if (inHole)
                {
                    if (ch is '{' or '(' or '[') depth++;
                    else if (ch is ')' or ']') depth--;
                    else if (ch == '}' && depth-- == 0) return i;
                }

                i++;
            }
        }

        return i;
    }

    // A string literal starting at i: its $ count, whether it is verbatim, its opening quote count and where its content starts.
    private static bool TryReadStringStart(char[] c, int i, out int dollars, out bool verbatim, out int quotes, out int contentStart)
    {
        dollars = 0;
        verbatim = false;
        quotes = 0;
        contentStart = i;

        // A $ or @ right after an identifier is no string prefix.
        if (c[i] is '$' or '@' && i > 0 && (char.IsLetterOrDigit(c[i - 1]) || c[i - 1] == '_')) return false;

        var j = i;
        while (j < c.Length && (c[j] == '$' || c[j] == '@'))
        {
            if (c[j] == '$') dollars++;
            else verbatim = true;
            j++;
        }

        while (j < c.Length && c[j] == '"')
        {
            quotes++;
            j++;
        }

        if (quotes == 0) return false;

        // "" is an empty string, not the start of a raw one.
        if (quotes == 2)
        {
            quotes = 1;
            j--;
        }

        contentStart = j;
        return true;
    }

    // Blanks a string's content from i and returns the index after its closing quotes.
    private static int ScanString(char[] c, int i, int dollars, bool verbatim, int quotes)
    {
        var raw = quotes >= 3;

        while (i < c.Length)
        {
            var ch = c[i];

            if (raw && ch == '"' && CountRun(c, i, '"') >= quotes)
            {
                return i + quotes;
            }

            if (raw is false && ch == '"')
            {
                if (verbatim && At(c, i + 1) == '"')
                {
                    Blank(c, i, i + 2);
                    i += 2;
                    continue;
                }

                return i + 1;
            }

            if (raw is false && verbatim is false && ch == '\\')
            {
                Blank(c, i, Math.Min(c.Length, i + 2));
                i += 2;
                continue;
            }

            if (raw is false && verbatim is false && ch == '\n') return i;

            if (dollars > 0 && ch == '{')
            {
                var run = CountRun(c, i, '{');
                var opening = raw ? dollars : 1;

                if (raw is false && run >= 2)
                {
                    Blank(c, i, i + 2);
                    i += 2;
                    continue;
                }

                if (run >= opening)
                {
                    var holeStart = i + run;
                    var holeEnd = ScanCode(c, holeStart, inHole: true);
                    var after = Math.Min(c.Length, holeEnd + (raw ? dollars : 1));
                    Blank(c, i, after);
                    i = after;
                    continue;
                }
            }

            if (ch != '\n') c[i] = ' ';
            i++;
        }

        return i;
    }

    private static char At(char[] c, int i) => i < c.Length ? c[i] : '\0';

    private static int CountRun(char[] c, int i, char ch)
    {
        var n = 0;
        while (i + n < c.Length && c[i + n] == ch) n++;
        return n;
    }

    private static int IndexOf(char[] c, string value, int start)
    {
        for (var i = start; i <= c.Length - value.Length; i++)
        {
            if (string.CompareOrdinal(new string(c, i, value.Length), value) == 0) return i;
        }

        return -1;
    }

    private static void Blank(char[] c, int from, int to)
    {
        for (var i = Math.Max(0, from); i < Math.Min(to, c.Length); i++)
        {
            if (c[i] != '\n') c[i] = ' ';
        }
    }

    /// <summary>
    /// Every Razor comment written inside a tag, between its attributes, with the name of the tag. The markup is read
    /// tag by tag: a comment between two tags is skipped whole, and a tag is read through its quoted values and its
    /// <c>@(...)</c> expressions to the <c>&gt;</c> that closes it. A <c>&lt;</c> followed by a letter that turns out
    /// not to open a tag (a comparison, a generic type in code) is dropped as soon as it meets what no tag holds.
    /// </summary>
    private static IEnumerable<(string Tag, int Comment)> FindCommentsInsideTags(string razor)
    {
        var i = 0;

        while (i < razor.Length)
        {
            if (string.CompareOrdinal(razor, i, "@*", 0, 2) == 0)
            {
                var end = razor.IndexOf("*@", i + 2, StringComparison.Ordinal);
                if (end < 0) yield break;
                i = end + 2;
                continue;
            }

            if (razor[i] == '<' && i + 1 < razor.Length && char.IsLetter(razor[i + 1]))
            {
                var comments = new List<int>();

                if (TryReadTag(razor, i, out var name, out var end, comments))
                {
                    foreach (var comment in comments) yield return (name, comment);

                    i = end + 1;
                    continue;
                }
            }

            i++;
        }
    }

    // Reads the tag opening at start (a '<' followed by its name) to the '>' closing it, collecting the Razor comments
    // between its attributes; false when what follows the '<' is no tag.
    private static bool TryReadTag(string razor, int start, out string name, out int end, List<int>? comments)
    {
        var j = start + 1;
        while (j < razor.Length && (char.IsLetterOrDigit(razor[j]) || razor[j] is '.' or ':' or '-' or '_')) j++;

        name = razor[(start + 1)..j];
        end = -1;

        while (j < razor.Length)
        {
            var ch = razor[j];

            if (ch == '>')
            {
                end = j;
                return true;
            }

            if (ch is '"' or '\'')
            {
                j = SkipQuotedValue(razor, j);
            }
            else if (string.CompareOrdinal(razor, j, "@*", 0, 2) == 0)
            {
                comments?.Add(j);
                var close = razor.IndexOf("*@", j + 2, StringComparison.Ordinal);
                if (close < 0) return false;
                j = close + 2;
            }
            else if (string.CompareOrdinal(razor, j, "@(", 0, 2) == 0)
            {
                j = SkipParentheses(razor, j + 1);
            }
            else if (ch is '<' or ';' or '{' or '}')
            {
                return false;
            }
            else
            {
                j++;
            }
        }

        return false;
    }

    // From the quote opening an attribute value to the index after the one closing it; an @(...) inside the value is
    // read whole, quotes and all.
    private static int SkipQuotedValue(string razor, int j)
    {
        var quote = razor[j++];

        while (j < razor.Length)
        {
            if (razor[j] == quote) return j + 1;

            j = string.CompareOrdinal(razor, j, "@(", 0, 2) == 0 ? SkipParentheses(razor, j + 1) : j + 1;
        }

        return j;
    }

    // From an opening parenthesis to the index after the one closing it, past the C# strings and characters inside.
    private static int SkipParentheses(string razor, int j)
    {
        var depth = 0;

        while (j < razor.Length)
        {
            var ch = razor[j];

            if (ch is '"' or '\'')
            {
                j++;
                while (j < razor.Length && razor[j] != ch)
                {
                    if (razor[j] == '\\') j++;
                    j++;
                }
            }
            else if (ch == '(')
            {
                depth++;
            }
            else if (ch == ')' && --depth == 0)
            {
                return j + 1;
            }

            j++;
        }

        return j;
    }

    // The markup with every Razor comment replaced by its line breaks, so every tag stays on the line it is on.
    private static string StripRazorComments(string razor) => RazorComment().Replace(razor, comment => new string('\n', comment.Value.Count(c => c == '\n')));

    private static bool IsNumberingExempt(string file)
    {
        var relative = Path.GetRelativePath(SourceFiles.GetPath("Demo", "Client", "Bit.BlazorUI.Demo.Client.Core"), Path.GetDirectoryName(file)!).Replace('\\', '/');

        return NumberingExemptions.Contains(relative);
    }

    // The build output, and everything of Bit.BlazorUI.Legacy: its package and its demo pages.
    private static bool IsExcluded(string path)
    {
        var segments = Path.GetRelativePath(SourceFiles.Root, path).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return segments.Any(s => s is "bin" or "obj" or "node_modules" or "Legacy" or "Bit.BlazorUI.Legacy");
    }

    private static string Relative(string path) => Path.GetRelativePath(SourceFiles.Root, path).Replace('\\', '/');

    private static int GetLine(string text, int index) => text.AsSpan(0, index).Count('\n') + 1;

    private static void Fail(List<string> offenders, string rule)
    {
        if (offenders.Count == 0) return;

        Assert.Fail($"{rule}{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    [GeneratedRegex(@"@\*.*?\*@", RegexOptions.Singleline)]
    private static partial Regex RazorComment();

    [GeneratedRegex(@"<DemoExample(?=[\s/>])")]
    private static partial Regex DemoExampleStart();

    [GeneratedRegex(@"<(?<tab>_\w+)(?=[\s/>])")]
    private static partial Regex TabTag();

    [GeneratedRegex(@"(?<=\s)(?<name>[\w@:.-]+)\s*=\s*""(?<value>[^""]*)""")]
    private static partial Regex TagAttribute();

    [GeneratedRegex(@"[\w\]?>]\s+(?<field>example(?<n>\d+)(?:RazorCode|CsharpCode|CodeFiles))\s*[=;]")]
    private static partial Regex SampleFieldDeclaration();

    [GeneratedRegex(@"#(?<id>example\d+)(?!\w)")]
    private static partial Regex ExampleLink();

    [GeneratedRegex(@"^@page\s+""(?<route>[^""]*)""", RegexOptions.Multiline)]
    private static partial Regex PageRoute();

    [GeneratedRegex(@"(?<=\s)SecondaryNames\s*=\s*""@\(\[.*?\]\)""|(?<=\s)SecondaryNames\s*=\s*""[^""]*""", RegexOptions.Singleline)]
    private static partial Regex SecondaryNamesAttribute();

    [GeneratedRegex(@"^SecondaryNames\s*=\s*""@\(\[(?<names>[^\]]*)\]\)""$", RegexOptions.Singleline)]
    private static partial Regex SecondaryNamesList();

    [GeneratedRegex(@"(?<=\s)SubEnums\s*=\s*""")]
    private static partial Regex SubEnumsAttribute();

    [GeneratedRegex(@"(?<=\s)Name\s*=\s*""(?<name>[^""]*)""")]
    private static partial Regex PageName();

    [GeneratedRegex(@"""(?<value>[^""]*)""")]
    private static partial Regex QuotedString();

    [GeneratedRegex(@"\{[^{}]*\}")]
    private static partial Regex InnermostBraces();

    [GeneratedRegex(@"\bType\s*=\s*""(?<type>\w+)(?<nullable>\??)""")]
    private static partial Regex TypeAssignment();

    [GeneratedRegex(@"\bName\s*=\s*""(?<value>[^""]*)""")]
    private static partial Regex NameAssignment();

    [GeneratedRegex(@"\bHref\s*=\s*""(?<value>[^""]*)""")]
    private static partial Regex HrefAssignment();

    [GeneratedRegex(@"\bLinkType\s*=\s*LinkType\.Link\b")]
    private static partial Regex LinkTypeLink();

    [GeneratedRegex(@"\bText\s*=\s*""(?<value>[^""]*)""")]
    private static partial Regex TextAssignment();

    [GeneratedRegex(@"\bUrl\s*=\s*""(?<value>[^""]*)""")]
    private static partial Regex UrlAssignment();

    [GeneratedRegex(@"\bAdditionalUrls\s*=\s*\[(?<urls>[^\]]*)\]")]
    private static partial Regex AdditionalUrlsAssignment();

    [GeneratedRegex(@"\bDescription\s*=\s*""(?<value>[^""]*)""")]
    private static partial Regex DescriptionAssignment();

    [GeneratedRegex(@"_notInheritedComponents\s*=\s*\[(?<names>[^\]]*)\]")]
    private static partial Regex NotInheritedComponents();
}
