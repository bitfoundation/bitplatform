using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// Pins the one way an input paints its placeholder (#13639): through the <c>input-placeholder</c> mixin of
/// functions.scss, which resets the opacity Firefox dims a placeholder with, from a public variable whose default is
/// the secondary foreground - the tier that clears the 4.5:1 of text on every surface - and again with the disabled
/// color in the component's disabled state.
/// </summary>
/// <remarks>
/// The rule used to be written by hand in every stylesheet, and the copies drifted: some lost the opacity reset, some
/// defaulted to the tertiary foreground, some kept the full-contrast hint on a disabled field, and some inputs were
/// left to the browser's grey altogether. These tests fail on a stylesheet that brings any of that back.
/// </remarks>
[TestClass]
public sealed class BitInputPlaceholderContractTests
{
    private const string Include = "@include input-placeholder(";

    // The HTML attribute an input or a textarea shows its hint through: lower case, so a component's own Placeholder
    // parameter does not count, and neither does a data-placeholder or an aria-placeholder.
    private static readonly Regex PlaceholderAttribute = new(@"(?<![\w-])placeholder=""", RegexOptions.Compiled);

    // A read of a public placeholder variable with its fallback token (--bit-TextField-placeholder-color,
    // --bit-PdfViewer-field-placeholder-color, ...).
    private static readonly Regex PublicPlaceholderRead = new(
        @"var\((--bit-[A-Z][A-Za-z]+-(?:[a-z]+-)*placeholder-color),\s*#\{\$clr-([a-z-]+)\}\)",
        RegexOptions.Compiled);

    // A documented public placeholder variable with the default its stylesheet header gives it.
    private static readonly Regex DocumentedPlaceholderDefault = new(
        @"(--bit-[A-Z][A-Za-z]+-(?:[a-z]+-)*placeholder-color)\b[^\n]*?(?:\n[ \t]*(?://|\*)[ \t]+[^\s-][^\n]*?)*?\(default: \$clr-([a-z-]+)\)",
        RegexOptions.Compiled);

    // The stylesheets whose fields are never rendered disabled, so they have no disabled placeholder to paint:
    // the panels of BitRichTextEditor are taken away while it is disabled, and a disabled BitFullCalendar is read-only,
    // which opens neither its event dialog nor anything else holding a field. The only field of BitCountryPicker is the
    // search box of its list, which a disabled picker never opens; the placeholder of the picker itself is a text rather
    // than a field's, greyed out with the rest of the disabled field.
    private static readonly string[] NeverDisabled = ["BitRichTextEditor.scss", "BitFullCalendar.scss", "BitCountryPicker.scss"];

    [TestMethod]
    public void TheMixinResetsTheBrowsersOpacity()
    {
        var functions = SourceFiles.StripScssComments(SourceFiles.Read("Bit.BlazorUI", "Styles", "functions.scss"));

        var mixin = SourceFiles.GetScssBlock(functions, "@mixin input-placeholder($color)");
        var declarations = SourceFiles.GetScssDeclarations(mixin, "&::placeholder");

        StringAssert.Contains(declarations, "opacity: 1;", "Firefox dims a placeholder with an opacity of its own, which the mixin has to reset.");
        StringAssert.Contains(declarations, "color: $color;");
    }

    [TestMethod]
    public void NoStylesheetPaintsAPlaceholderByHand()
    {
        var offenders = EnumerateComponentStylesheets()
            .Where(file => SourceFiles.StripScssComments(SourceFiles.ReadFullPath(file)).Contains("::placeholder", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .ToArray();

        Assert.AreEqual(0, offenders.Length,
            "A component stylesheet writes a ::placeholder rule of its own; paint it with @include input-placeholder(...) from " +
            "functions.scss, which also resets the opacity Firefox dims it with: " + string.Join(", ", offenders));
    }

    [TestMethod]
    public void EveryComponentRenderingAPlaceholderPaintsIt()
    {
        var offenders = new List<string>();
        var checkedCount = 0;

        foreach (var markup in EnumerateComponentMarkup())
        {
            if (PlaceholderAttribute.IsMatch(SourceFiles.ReadFullPath(markup)) is false) continue;

            checkedCount++;

            var stylesheets = OwnStylesheets(markup);

            if (stylesheets.Any(file => SourceFiles.StripScssComments(SourceFiles.ReadFullPath(file)).Contains(Include, StringComparison.Ordinal))) continue;

            offenders.Add(Path.GetFileName(markup));
        }

        Assert.IsTrue(checkedCount > 10, $"Only {checkedCount} component files render a placeholder; the scan no longer finds the inputs.");

        Assert.AreEqual(0, offenders.Count,
            "A component renders an input with a placeholder that its stylesheet leaves to the browser's grey, which is not " +
            "derived from the theme and misses 4.5:1 on most surfaces; paint it with @include input-placeholder(...): " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void APlaceholderDefaultsToTheSecondaryForeground()
    {
        var offenders = new List<string>();

        foreach (var file in EnumerateComponentStylesheets())
        {
            var source = SourceFiles.ReadFullPath(file);
            var stylesheet = SourceFiles.StripScssComments(source);
            var name = Path.GetFileName(file);

            foreach (var argument in IncludeArguments(stylesheet))
            {
                if (argument.Contains("fg-ter", StringComparison.Ordinal))
                {
                    offenders.Add($"{name}: {Include}{argument})");
                }
            }

            foreach (Match read in PublicPlaceholderRead.Matches(stylesheet))
            {
                if (read.Groups[2].Value != "fg-sec")
                {
                    offenders.Add($"{name}: {read.Groups[1].Value} falls back to $clr-{read.Groups[2].Value}");
                }
            }

            foreach (Match documented in DocumentedPlaceholderDefault.Matches(source))
            {
                if (documented.Groups[2].Value != "fg-sec")
                {
                    offenders.Add($"{name}: {documented.Groups[1].Value} is documented with the default $clr-{documented.Groups[2].Value}");
                }
            }
        }

        Assert.AreEqual(0, offenders.Count,
            "A placeholder is held to the 4.5:1 of text, which only the secondary foreground clears on every surface; " +
            "the tertiary one misses it on bg-ter: " + string.Join("; ", offenders));
    }

    [TestMethod]
    public void ADisabledFieldGreysItsPlaceholderOut()
    {
        var offenders = new List<string>();

        foreach (var file in EnumerateComponentStylesheets())
        {
            var name = Path.GetFileName(file);

            if (NeverDisabled.Contains(name, StringComparer.Ordinal)) continue;

            var arguments = IncludeArguments(SourceFiles.StripScssComments(SourceFiles.ReadFullPath(file))).ToArray();

            if (arguments.Length == 0) continue;

            if (arguments.Any(IsDisabledColor) is false)
            {
                offenders.Add(name);
            }
        }

        Assert.AreEqual(0, offenders.Count,
            "A stylesheet paints a placeholder but never with its disabled color, so the hint of an empty disabled field " +
            "stays at full contrast and reads as live; include input-placeholder(...) in the disabled state too: " +
            string.Join(", ", offenders));
    }

    private static bool IsDisabledColor(string argument)
        => Regex.IsMatch(argument, @"fg-dis|disabled-color|dis-clr|clr-dis", RegexOptions.CultureInvariant);

    private static IEnumerable<string> IncludeArguments(string stylesheet)
    {
        var index = 0;

        while ((index = stylesheet.IndexOf(Include, index, StringComparison.Ordinal)) >= 0)
        {
            var start = index + Include.Length;
            var depth = 1;
            var end = start;

            for (; end < stylesheet.Length && depth > 0; end++)
            {
                if (stylesheet[end] == '(') depth++;
                else if (stylesheet[end] == ')') depth--;
            }

            yield return stylesheet[start..(end - 1)];

            index = end;
        }
    }

    // The stylesheets that style a component's markup: those of its own folder, or of the nearest folder above it
    // that has one (a view or a dialog of BitFullCalendar is styled by the calendar's stylesheet).
    private static string[] OwnStylesheets(string markup)
    {
        for (var directory = Path.GetDirectoryName(markup); directory is not null; directory = Path.GetDirectoryName(directory))
        {
            var stylesheets = Directory.GetFiles(directory, "*.scss");

            if (stylesheets.Length > 0) return stylesheets;

            if (string.Equals(Path.GetFileName(directory), "Components", StringComparison.Ordinal)) break;
        }

        return [];
    }

    private static IEnumerable<string> EnumerateComponentMarkup()
        => ComponentFolders().SelectMany(folder => Directory.EnumerateFiles(folder, "*.razor", SearchOption.AllDirectories));

    private static IEnumerable<string> EnumerateComponentStylesheets()
        => ComponentFolders().SelectMany(folder => Directory.EnumerateFiles(folder, "*.scss", SearchOption.AllDirectories));

    private static IEnumerable<string> ComponentFolders()
        => new[] { "Bit.BlazorUI", "Bit.BlazorUI.Extras" }.Select(project => SourceFiles.GetDirectory(project, "Components"));
}
