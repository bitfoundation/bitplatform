using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils;

/// <summary>
/// Pins how a disabled component says so in its markup, which an app's stylesheets, scripts and tests are written
/// against: the bit-dis class on the root of every component that is Disabled, and on every native control it renders
/// disabled the disabled attribute together with aria-disabled="true" - so one [aria-disabled="true"] selector or
/// locator finds a disabled control whether it is a native button, an input or a part conveyed through aria-disabled
/// alone (a loading button, AllowDisabledFocus, a link), and :disabled still finds the native ones.
/// </summary>
/// <remarks>
/// The source of every component is read as well as a render of each, since most of the controls a component renders
/// disabled - the buttons of a calendar, of a toolbar, of an open popup - are drawn only in states a plain render of it
/// never reaches.
/// </remarks>
[TestClass]
public class BitDisabledMarkupContractTests : BunitTestContext
{
    private const string ROOT = "data-splat-root";

    // The elements HTML defines the disabled attribute on.
    private static readonly HashSet<string> NativeControls = new(StringComparer.OrdinalIgnoreCase)
    {
        "button", "input", "select", "textarea", "fieldset", "option", "optgroup",
    };

    private static readonly Regex TagStart = new(@"<(?<name>[a-zA-Z][\w.:-]*)", RegexOptions.Compiled);

    private static readonly Regex DisabledAttribute = new(@"(?<![\w-])disabled\s*=", RegexOptions.Compiled);

    // What the disabled attribute of a native control is written from: one name - a parameter, a property, a local -
    // read as it is, negated or compared with a boolean literal. Anything more is computed into a name first.
    private static readonly Regex OneName = new(@"^!?[A-Za-z_]\w*(\.[A-Za-z_]\w*)*( is (true|false))?$", RegexOptions.Compiled);

    private static readonly Regex BareName = new(@"^[A-Za-z_]\w*$", RegexOptions.Compiled);

    private static readonly Regex SplattedAriaDisabled = new(@"^(?:[\w.]+\.)?GetInputAriaDisabled\((?<condition>.+)\)$", RegexOptions.Compiled);

    // A RenderTreeBuilder call writing the disabled attribute, in the markup a component builds in C#.
    private static readonly Regex BuilderDisabledAttribute = new(@"\.AddAttribute\(\s*[^,]+,\s*""disabled""\s*,", RegexOptions.Compiled);

    public static IEnumerable<object[]> Components =>
        BitDisabledActivationContractTests.Components.Concat(BitDisabledActivationContractTests.ContentHosts);

    // The carousels, the theme and the accent color switchers read services of the library's own.
    [TestInitialize]
    public void AddServices() => Context.Services.AddBitBlazorUIExtrasServices();

    [TestMethod]
    public void EveryNativeControlWrittenDisabledShouldAlsoBeAriaDisabled()
    {
        var controls = ReadComponentTags()
            .Where(t => NativeControls.Contains(t.Name) && DisabledAttribute.IsMatch(t.Tag))
            .ToArray();

        Assert.IsTrue(controls.Length > 300, $"Only {controls.Length} native controls writing disabled were found in the components' markup.");

        // A hidden input only carries a value to the form; nothing reaches it, so it has no state to report.
        var offenders = controls
            .Where(t => t.Tag.Contains("aria-disabled", StringComparison.Ordinal) is false)
            .Where(t => IsHiddenInput(t.Tag) is false)
            .Select(t => $"{t.File}:{t.Line} <{t.Name}>")
            .ToArray();

        Assert.AreEqual(0, offenders.Length,
                        "These native controls write disabled without aria-disabled, which every disabled native control of the " +
                        $"library writes beside it (see src/BlazorUI/CLAUDE.md):\n{string.Join("\n", offenders)}");
    }

    [TestMethod]
    public void EveryNativeControlABuilderWritesDisabledShouldAlsoBeAriaDisabled()
    {
        // The markup a component builds in C# - BitElement's, a toolbar's, a markdown task box's - is held to the same
        // rule as its .razor markup: aria-disabled is written into the same element as the disabled attribute.
        var writes = ReadBuilderSources()
            .SelectMany(source => BuilderDisabledAttribute.Matches(source.Code).Select(match => (source.File, source.Code, match.Index)))
            .ToArray();

        Assert.IsTrue(writes.Length >= 3, $"Only {writes.Length} RenderTreeBuilder writes of the disabled attribute were found.");

        var offenders = writes
            .Where(write =>
            {
                var open = write.Code.LastIndexOf(".OpenElement(", write.Index, StringComparison.Ordinal);
                var close = write.Code.IndexOf(".CloseElement(", write.Index, StringComparison.Ordinal);
                var element = write.Code[Math.Max(open, 0)..(close < 0 ? write.Code.Length : close)];

                return element.Contains("\"aria-disabled\"", StringComparison.Ordinal) is false;
            })
            .Select(write => $"{write.File}:{write.Code.AsSpan(0, write.Index).Count('\n') + 1}")
            .ToArray();

        Assert.AreEqual(0, offenders.Length,
                        "These RenderTreeBuilder writes of the disabled attribute leave aria-disabled out of the element they write it " +
                        $"on (see src/BlazorUI/CLAUDE.md):\n{string.Join("\n", offenders)}");
    }

    [TestMethod]
    public void EveryNativeControlShouldReadItsDisabledStateFromOneName()
    {
        // The disabled attribute and the aria-disabled beside it say the same thing, so they read it from the same place:
        // a condition written out twice is evaluated twice - an app's delegate called twice a row, per render - and is
        // two copies the next edit can take apart. One computed into a local or a property is neither.
        var offenders = ReadComponentTags()
            .Where(t => NativeControls.Contains(t.Name) && IsHiddenInput(t.Tag) is false)
            .Select(t => (Tag: t, Disabled: ReadAttribute(t.Tag, "disabled")))
            .Where(c => c.Disabled is not null && OneName.IsMatch(Normalize(c.Disabled)) is false)
            .Select(c => $"{c.Tag.File}:{c.Tag.Line} <{c.Tag.Name} disabled=\"{c.Disabled}\">")
            .ToArray();

        Assert.AreEqual(0, offenders.Length,
                        "These native controls compute their disabled state inline; compute it once into a local or a property that " +
                        $"both the disabled and the aria-disabled attribute read (see src/BlazorUI/CLAUDE.md):\n{string.Join("\n", offenders)}");
    }

    [TestMethod]
    public void EveryAriaDisabledShouldCoverTheDisabledAttributeBesideIt()
    {
        // aria-disabled may say more than the disabled attribute (a loading button, a read-only field), never less: its
        // condition names the disabled one among the states it is true for, directly or through the locals and the
        // properties it is computed from.
        var controls = ReadComponentTags()
            .Where(t => NativeControls.Contains(t.Name) && IsHiddenInput(t.Tag) is false)
            .Select(t => (Tag: t, Disabled: ReadAttribute(t.Tag, "disabled"), AriaDisabled: ReadAttribute(t.Tag, "aria-disabled")))
            .Where(c => c.Disabled is not null && c.AriaDisabled is not null)
            .ToArray();

        Assert.IsTrue(controls.Length > 300, $"Only {controls.Length} native controls writing both attributes were found.");

        var offenders = controls
            .Where(c =>
            {
                var condition = ReadAriaDisabledCondition(c.AriaDisabled!, c.Tag, 0);

                return condition is null || IsCovered(Normalize(c.Disabled!), ReadDisjuncts(condition, c.Tag, 0), c.Tag, 0) is false;
            })
            .Select(c => $"{c.Tag.File}:{c.Tag.Line} <{c.Tag.Name} disabled=\"{c.Disabled}\" aria-disabled=\"{c.AriaDisabled}\">")
            .ToArray();

        Assert.AreEqual(0, offenders.Length,
                        "The aria-disabled of these native controls is not true for every state their disabled attribute is (see " +
                        $"src/BlazorUI/CLAUDE.md):\n{string.Join("\n", offenders)}");
    }

    [TestMethod]
    public void NoElementOtherThanANativeControlShouldWriteTheDisabledAttribute()
    {
        // HTML defines disabled on the form controls alone: on any other element it reports nothing to assistive
        // technologies, takes nothing out of the tab order and matches no :disabled - aria-disabled is what says it.
        var offenders = ReadComponentTags()
            .Where(t => char.IsLower(t.Name[0]) && NativeControls.Contains(t.Name) is false && DisabledAttribute.IsMatch(t.Tag))
            .Select(t => $"{t.File}:{t.Line} <{t.Name}>")
            .ToArray();

        Assert.AreEqual(0, offenders.Length,
                        $"These elements write the disabled attribute, which HTML defines on the form controls only:\n{string.Join("\n", offenders)}");
    }

    [TestMethod]
    [DynamicData(nameof(Components))]
    public void DisabledComponentShouldCarryTheDisabledClassOnItsRoot(Type type)
    {
        var root = Render(type, disabled: true).Find($"[{ROOT}]");

        Assert.IsTrue(root.ClassList.Contains("bit-dis"), $"The root of a disabled {type.Name} has no bit-dis class.");
    }

    [TestMethod]
    [DynamicData(nameof(Components))]
    public void EnabledComponentShouldNotCarryTheDisabledClassOnItsRoot(Type type)
    {
        var root = Render(type, disabled: false).Find($"[{ROOT}]");

        Assert.IsFalse(root.ClassList.Contains("bit-dis"), $"The root of an enabled {type.Name} has the bit-dis class.");
    }

    [TestMethod]
    [DynamicData(nameof(Components))]
    public void DisabledComponentShouldPairEveryDisabledControlWithAriaDisabled(Type type)
    {
        var component = Render(type, disabled: true);

        var offenders = component.FindAll("[disabled]:not(input[type=hidden])")
                                 .Where(element => element.GetAttribute("aria-disabled") != "true")
                                 .Select(element => element.TagName.ToLowerInvariant() + (element.ClassName is { Length: > 0 } c ? "." + c.Replace(' ', '.') : ""))
                                 .ToArray();

        Assert.AreEqual(0, offenders.Length,
                        $"A disabled {type.Name} renders these elements disabled without aria-disabled=\"true\":\n{string.Join("\n", offenders)}");
    }

    private IRenderedComponent<BitParams> Render(Type type, bool disabled)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, []);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent(0, type);
                builder.AddAttribute(1, nameof(BitComponentBase.Disabled), disabled);
                builder.AddAttribute(2, ROOT, "");
                builder.CloseComponent();
            });
        });
    }

    private static bool IsHiddenInput(string tag) => tag.Contains("type=\"hidden\"", StringComparison.Ordinal);

    // The value of an attribute written in a tag, read to the quote that closes it outside the parentheses of a Razor
    // expression, whose own string literals hold quotes too.
    private static string? ReadAttribute(string tag, string name)
    {
        var match = Regex.Match(tag, $@"(?<![\w-]){Regex.Escape(name)}\s*=\s*""");
        if (match.Success is false) return null;

        var depth = 0;
        var end = match.Index + match.Length;

        for (; end < tag.Length; end++)
        {
            var c = tag[end];

            if (c == '(') depth++;
            else if (c == ')') depth--;
            else if (c == '"' && depth == 0) break;
        }

        return tag[(match.Index + match.Length)..end];
    }

    // An expression with the Razor transition, the parentheses around the whole of it and the line breaks in it taken
    // away, so two spellings of one condition compare equal.
    private static string Normalize(string expression)
    {
        var normalized = Regex.Replace(expression.Trim(), @"\s+", " ");

        if (normalized.StartsWith('@')) normalized = normalized[1..].Trim();

        while (normalized.StartsWith('(') && ClosingParenthesis(normalized, 0) == normalized.Length - 1)
        {
            normalized = normalized[1..^1].Trim();
        }

        return normalized;
    }

    private static int ClosingParenthesis(string expression, int open)
    {
        var depth = 0;
        char? quote = null;

        for (var i = open; i < expression.Length; i++)
        {
            var c = expression[i];

            if (quote is not null) { if (c == quote) quote = null; continue; }
            if (c is '"' or '\'') { quote = c; continue; }
            if (c == '(') depth++;
            else if (c == ')' && --depth == 0) return i;
        }

        return -1;
    }

    // The operands of an expression's top-level operator: "a || (b && c)" splits on "||" into "a" and "b && c".
    private static List<string> SplitTopLevel(string expression, string @operator)
    {
        var parts = new List<string>();
        var depth = 0;
        var start = 0;
        char? quote = null;

        for (var i = 0; i < expression.Length; i++)
        {
            var c = expression[i];

            if (depth == 0 && quote is null && string.CompareOrdinal(expression, i, @operator, 0, @operator.Length) == 0)
            {
                parts.Add(Normalize(expression[start..i]));
                start = i + @operator.Length;
                i = start - 1;
                continue;
            }

            if (quote is not null) { if (c == quote) quote = null; continue; }
            if (c is '"' or '\'') { quote = c; continue; }
            if (c == '(') depth++;
            else if (c == ')') depth--;
        }

        parts.Add(Normalize(expression[start..]));

        return parts;
    }

    // The condition an aria-disabled is "true" for: the argument of GetInputAriaDisabled, the condition of a
    // `C ? "true" : ...`, or the one of the local or property it is read from. Null when it is written any other way.
    private static string? ReadAriaDisabledCondition(string value, ComponentTag tag, int depth)
    {
        var expression = Normalize(value);

        var splatted = SplattedAriaDisabled.Match(expression);
        if (splatted.Success) return Normalize(splatted.Groups["condition"].Value);

        var ternary = SplitTopLevel(expression, " ? \"true\" :");
        if (ternary.Count == 2) return ternary[0];

        if (depth < 4 && BareName.IsMatch(expression) && FindDefinition(expression, tag) is { } definition)
        {
            return ReadAriaDisabledCondition(definition, tag, depth + 1);
        }

        return null;
    }

    // The states a condition is true for: its top-level disjuncts, each one also expanded into the disjuncts of the local
    // or property it names.
    private static HashSet<string> ReadDisjuncts(string condition, ComponentTag tag, int depth)
    {
        var disjuncts = new HashSet<string>(StringComparer.Ordinal);

        foreach (var part in SplitTopLevel(Normalize(condition), "||"))
        {
            disjuncts.Add(part);

            if (depth < 4 && BareName.IsMatch(part) && FindDefinition(part, tag) is { } definition)
            {
                disjuncts.UnionWith(ReadDisjuncts(definition, tag, depth + 1));
            }
        }

        return disjuncts;
    }

    // Whether a set of disjuncts is true whenever the disabled condition is: it names the condition, or every disjunct of
    // it, or - for a conjunction - one of its terms.
    private static bool IsCovered(string disabled, HashSet<string> disjuncts, ComponentTag tag, int depth)
    {
        if (disjuncts.Contains(disabled)) return true;

        if (depth >= 4) return false;

        var expression = BareName.IsMatch(disabled) && FindDefinition(disabled, tag) is { } definition ? Normalize(definition) : disabled;

        var alternatives = SplitTopLevel(expression, "||");
        if (alternatives.Count > 1) return alternatives.All(alternative => IsCovered(alternative, disjuncts, tag, depth + 1));

        var terms = SplitTopLevel(expression, "&&");
        if (terms.Count > 1) return terms.Any(term => IsCovered(term, disjuncts, tag, depth + 1));

        return expression != disabled && IsCovered(expression, disjuncts, tag, depth + 1);
    }

    // What a name stands for where the tag reads it: the nearest local of the markup declared above the tag, or else an
    // expression-bodied member of the component - in its markup's @code or in any of the partial class's files.
    private static string? FindDefinition(string name, ComponentTag tag)
    {
        var local = new Regex($@"(?<![\w.])(?:var|bool)\s+{name}\s*=\s*(?<expression>[^;]+);");

        var declared = local.Matches(tag.Markup).Where(m => m.Index < tag.Index).LastOrDefault();
        if (declared is not null) return declared.Groups["expression"].Value;

        var member = new Regex($@"(?<![\w.]){name}\s*=>\s*(?<expression>[^;]+);");

        foreach (var code in ReadComponentCode(tag.FullPath).Prepend(tag.Markup))
        {
            var match = member.Match(code);
            if (match.Success) return match.Groups["expression"].Value;
        }

        return null;
    }

    // The C# files of the component a markup file belongs to: its code-behind and the other files of its partial class,
    // which share its name as their prefix (BitRichTextEditor.razor.cs, BitRichTextEditor.Tables.cs, ...).
    private static IEnumerable<string> ReadComponentCode(string markupPath)
    {
        var folder = Path.GetDirectoryName(markupPath)!;
        var component = Path.GetFileNameWithoutExtension(markupPath);

        return Directory.EnumerateFiles(folder, $"{component}.*cs").Select(SourceFiles.ReadFullPath);
    }

    private sealed record ComponentTag(string File, string FullPath, string Markup, int Index, int Line, string Name, string Tag);

    private static readonly string[] PackageFolders = ["Bit.BlazorUI", "Bit.BlazorUI.Extras", "Bit.BlazorUI.Legacy"];

    private static IEnumerable<string> EnumeratePackageFiles(string searchPattern)
    {
        return PackageFolders
            .Select(folder => SourceFiles.GetDirectory(folder))
            .SelectMany(folder => Directory.EnumerateFiles(folder, searchPattern, SearchOption.AllDirectories))
            .Where(file =>
            {
                var relative = Path.GetRelativePath(SourceFiles.Root, file).Replace('\\', '/');

                return relative.Contains("/bin/", StringComparison.Ordinal) is false && relative.Contains("/obj/", StringComparison.Ordinal) is false;
            });
    }

    // The C# sources of the packages that build markup with a RenderTreeBuilder.
    private static IEnumerable<(string File, string Code)> ReadBuilderSources()
    {
        return EnumeratePackageFiles("*.cs")
            .Select(file => (File: Path.GetRelativePath(SourceFiles.Root, file).Replace('\\', '/'), Code: SourceFiles.ReadFullPath(file)))
            .Where(source => source.Code.Contains(".OpenElement(", StringComparison.Ordinal));
    }

    // Every opening tag of the packages' markup, with the file and line it starts on. A tag ends at the first '>'
    // outside its attribute values and the parentheses of a Razor expression - never at the one of a '=>'.
    private static IEnumerable<ComponentTag> ReadComponentTags()
    {
        var files = EnumeratePackageFiles("*.razor").ToArray();

        Assert.IsTrue(files.Length > 100, $"Only {files.Length} component markup files were found.");

        foreach (var file in files)
        {
            var markup = SourceFiles.ReadFullPath(file);
            var name = Path.GetRelativePath(SourceFiles.Root, file).Replace('\\', '/');

            foreach (Match match in TagStart.Matches(markup))
            {
                var end = match.Index + match.Length;
                var depth = 0;
                char? quote = null;

                for (; end < markup.Length; end++)
                {
                    var c = markup[end];

                    if (c == '(') depth++;
                    else if (c == ')') depth = Math.Max(0, depth - 1);
                    else if (quote is not null) { if (c == quote) quote = null; }
                    else if (c is '"' or '\'') quote = c;
                    else if (c == '>' && depth == 0 && markup[end - 1] != '=') break;
                }

                var tag = markup[match.Index..Math.Min(end + 1, markup.Length)];
                var line = markup.AsSpan(0, match.Index).Count('\n') + 1;

                yield return new ComponentTag(name, file, markup, match.Index, line, match.Groups["name"].Value, tag);
            }
        }
    }
}
