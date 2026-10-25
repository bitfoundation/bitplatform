using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// Pins that every caption naming a form field takes its weight from the theme's field-label token
/// (<c>$tg-field-label-font-weight</c>), the one BitLabel reads too, and nothing else.
/// </summary>
/// <remarks>
/// Fluent captions a field semibold, while Fluent 2, Material and Cupertino caption it regular, so a caption that
/// reads any weight of its own - the semibold step, another step, <c>$tg-font-weight</c> or a literal - stays that
/// weight under those presets beside every other input and BitLabel. Reading the token through the theme also keeps
/// a caption following a <c>FontWeights</c> override of a BitThemeProvider, since the alias is re-declared for the
/// overlay (the <c>typography</c> group).
/// <para>
/// Every class a component stylesheet names like a caption (a part ending in <c>-lbl</c>, <c>-rlb</c> or
/// <c>-label</c>) is classified below, either as a field's caption or as a label that is not one (a button's text, a
/// tag's, a pager's), so a new input's caption cannot slip past the contract unlisted. Every rule that
/// targets a field caption is read - a grouped selector, a nested one, one inside <c>@media</c> - and each of its
/// <c>font-weight</c> declarations has to read the token.
/// </para>
/// </remarks>
[TestClass]
public sealed class BitFieldLabelFontWeightContractTests
{
    private const string FieldLabelToken = "$tg-field-label-font-weight";

    // The captions that name a form field, with the stylesheet each is styled in.
    private static readonly (string Caption, string Path)[] FieldCaptions =
    [
        (".bit-chg-lbl", "Bit.BlazorUI/Components/Inputs/ChoiceGroup/BitChoiceGroup.scss"),
        (".bit-ctp-lbl", "Bit.BlazorUI/Components/Inputs/CircularTimePicker/BitCircularTimePicker.scss"),
        (".bit-clp-lbl", "Bit.BlazorUI/Components/Inputs/ColorPicker/BitColorPicker.scss"),
        (".bit-dtp-lbl", "Bit.BlazorUI/Components/Inputs/DatePicker/BitDatePicker.scss"),
        (".bit-dtrp-lbl", "Bit.BlazorUI/Components/Inputs/DateRangePicker/BitDateRangePicker.scss"),
        (".bit-drp-lbl", "Bit.BlazorUI/Components/Inputs/Dropdown/BitDropdown.scss"),
        (".bit-nfl-lbl", "Bit.BlazorUI/Components/Inputs/NumberField/BitNumberField.scss"),
        (".bit-otp-lbl", "Bit.BlazorUI/Components/Inputs/OtpInput/BitOtpInput.scss"),
        (".bit-rtg-lbl", "Bit.BlazorUI/Components/Inputs/Rating/BitRating.scss"),
        (".bit-srb-lbl", "Bit.BlazorUI/Components/Inputs/SearchBox/BitSearchBox.scss"),
        (".bit-sld-lbl", "Bit.BlazorUI/Components/Inputs/Slider/BitSlider.scss"),
        (".bit-tgi-lbl", "Bit.BlazorUI/Components/Inputs/TagsInput/BitTagsInput.scss"),
        (".bit-tfl-lbl", "Bit.BlazorUI/Components/Inputs/TextField/BitTextField.scss"),
        (".bit-tpc-lbl", "Bit.BlazorUI/Components/Inputs/TimePicker/BitTimePicker.scss"),
        (".bit-tgl-lbl", "Bit.BlazorUI/Components/Inputs/Toggle/BitToggle.scss"),
        (".bit-bfc-field-label", "Bit.BlazorUI.Extras/Components/FullCalendar/BitFullCalendar.scss"),
        (".bit-cnp-lbl", "Bit.BlazorUI.Extras/Components/CountryPicker/BitCountryPicker.scss"),
        (".bit-cnp-rlb", "Bit.BlazorUI.Extras/Components/CountryPicker/BitCountryPicker.scss"),
        (".bit-tzp-lbl", "Bit.BlazorUI.Extras/Components/TimeZonePicker/BitTimeZonePicker.scss"),
        (".bit-tzp-rlb", "Bit.BlazorUI.Extras/Components/TimeZonePicker/BitTimeZonePicker.scss"),
        (".bit-mde-lbl", "Bit.BlazorUI.Extras/Components/MarkdownEditor/BitMarkdownEditor.scss"),
        (".bit-phi-lbl", "Bit.BlazorUI.Extras/Components/PhoneInput/BitPhoneInput.scss"),
        (".bit-phi-rlb", "Bit.BlazorUI.Extras/Components/PhoneInput/BitPhoneInput.scss"),
        (".bit-rte-lbl", "Bit.BlazorUI.Extras/Components/RichTextEditor/BitRichTextEditor.scss"),
    ];

    // The classes named like a caption that caption no field, so their weight is their own component's decision.
    private static readonly HashSet<string> OtherLabels =
    [
        ".bit-acb-lbl", // the text of an action button
        ".bit-btn-lbl", // the loading text of a button
        ".bit-tgb-lbl", // the loading text of a toggle button
        ".bit-chb-lbl", // the clickable row of a checkbox: the box and its text, read at the body weight
        ".bit-fin-lbl", // the browse button of a file input
        ".bit-upl-lbl", // the browse button / drop area of a file upload
        ".bit-ldn-lbl", // the text under a loading indicator
        ".bit-prb-lbl", // the text of a progress bar
        ".bit-pgn-lbl", // the page text of a pagination
        ".bit-tag-lbl", // the text of a tag
        ".bit-dtg-chooser-label", // a checkbox row of a data grid's column chooser
        ".bit-pdv-page-label", // the page number of a pdf viewer
        ".bit-pdv-thumb-label", // the number under a pdf viewer's thumbnail
        ".bit-bfc-bg-event-label", // the title of a full calendar's background event
        ".bit-bfc-dropdown-label", // the heading of a section of a full calendar's settings menu
        ".bit-bfc-dtp-month-label", // the month a full calendar's date picker shows
        ".bit-bfc-event-detail-label", // the term of a row of a full calendar's event details
        ".bit-bfc-repeat-dates-label", // the heading of the skipped / added dates of a repeating event
        ".bit-bfc-time-slot-label", // the time of a full calendar's slot
        ".bit-bfc-timeline-label", // the time on a full calendar's timeline
        ".bit-bfc-tl-time-cell-label", // the time of a full calendar's timeline cell
    ];

    // A class named like a caption: a part of a component whose name ends in lbl, rlb or label. The name must end
    // there: --bit-btn-lbl-fontsize is no class, .bit-tfl-lbl-x is another part.
    private static readonly Regex CaptionClass = new(
        @"\.bit-[a-z0-9]+(?:-[a-z0-9]+)*-(?:lbl|rlb|label)(?![-\w])",
        RegexOptions.Compiled);

    // The font-weight property itself - never a custom property whose name ends in font-weight - with its value,
    // interpolations (#{...}) included.
    private static readonly Regex FontWeight = new(
        @"(?<![-\w])font-weight\s*:\s*(?<value>(?:#\{[^}]*\}|[^;{}])+)",
        RegexOptions.Compiled);

    // A weight of its own, once the token is taken out of the value: a typography step or the base weight, a number
    // or a keyword.
    private static readonly Regex OtherWeight = new(
        @"\$tg-(?:fw-|font-weight)|(?<![-\w])[1-9]00(?![-\w])|(?<![-\w])(?:normal|bold|bolder|lighter|inherit)(?![-\w])",
        RegexOptions.Compiled);

    [TestMethod]
    public void EveryCaptionClassShouldBeClassified()
    {
        var fieldCaptions = FieldCaptions.Select(c => c.Caption).ToHashSet();
        var found = new Dictionary<string, string>();

        foreach (var file in EnumerateComponentStylesheets())
        {
            var stylesheet = SourceFiles.StripScssComments(SourceFiles.ReadFullPath(file));

            foreach (var rule in SourceFiles.GetScssRules(stylesheet))
            {
                foreach (Match match in CaptionClass.Matches(rule.Header))
                {
                    found.TryAdd(match.Value, Path.GetRelativePath(SourceFiles.Root, file));
                }
            }
        }

        var unclassified = found.Where(c => fieldCaptions.Contains(c.Key) is false && OtherLabels.Contains(c.Key) is false)
                                .Select(c => $"{c.Value}: {c.Key}")
                                .ToList();

        Assert.AreEqual(0, unclassified.Count,
            "These classes are named like a caption but are in neither list of this test - a field's caption goes in " +
            "FieldCaptions, any other label in OtherLabels:\n" + string.Join("\n", unclassified));

        var stale = fieldCaptions.Concat(OtherLabels).Where(c => found.ContainsKey(c) is false).ToList();

        Assert.AreEqual(0, stale.Count, "No component stylesheet styles these listed classes any more:\n" + string.Join("\n", stale));
    }

    [TestMethod]
    public void FieldCaptionShouldTakeItsWeightFromTheFieldLabelToken()
    {
        var offenders = new List<string>();

        foreach (var (caption, path) in FieldCaptions)
        {
            var stylesheet = SourceFiles.StripScssComments(SourceFiles.Read(path.Split('/')));
            var weighted = 0;

            foreach (var rule in SourceFiles.GetScssRules(stylesheet))
            {
                if (ResolveSelectors(rule).Any(selector => Targets(selector, caption)) is false) continue;

                var declarations = SourceFiles.GetScssDeclarations(stylesheet[rule.Index..], rule.Header);

                foreach (Match weight in FontWeight.Matches(declarations))
                {
                    weighted++;

                    var value = weight.Groups["value"].Value.Trim();

                    if (value.Contains(FieldLabelToken) is false || OtherWeight.IsMatch(value.Replace(FieldLabelToken, "")))
                    {
                        offenders.Add($"{path}: {caption} in \"{rule.Header}\" has font-weight: {value}");
                    }
                }
            }

            if (weighted == 0)
            {
                offenders.Add($"{path}: no rule sets the font-weight of {caption}");
            }
        }

        Assert.AreEqual(0, offenders.Count,
            $"Every font-weight of a field caption reads {FieldLabelToken} and nothing else:\n" + string.Join("\n", offenders));
    }

    // Whether the selector's subject - its last compound, less any :not() - carries the caption class. A rule whose
    // subject is a descendant of the caption (.bit-chb-lbl .bit-chb-box) or its pseudo-element (the required
    // asterisk of ::after) styles something else.
    private static bool Targets(string selector, string caption)
    {
        var subject = LastCompound(selector);

        if (subject.Contains("::")) return false;

        subject = Regex.Replace(subject, @":not\([^()]*\)", "");

        return Regex.IsMatch(subject, Regex.Escape(caption) + @"(?![-\w])");
    }

    // The selectors a rule matches, its ancestors' folded in the way Sass nests them: & stands for the parent, and a
    // selector without one is a descendant of it. An at-rule (@media, @supports, @include, @mixin) adds no selector.
    private static IEnumerable<string> ResolveSelectors(SourceFiles.ScssRule rule)
    {
        if (rule.Header.StartsWith('@')) return [];

        IEnumerable<string> resolved = [""];

        foreach (var header in rule.Ancestors.Append(rule.Header))
        {
            if (header.StartsWith('@')) continue;

            var parents = resolved.ToList();

            resolved = SplitSelectorList(header).SelectMany(child => parents.Select(parent =>
                parent.Length == 0 ? child.Replace("&", "")
                : child.Contains('&') ? child.Replace("&", parent)
                : $"{parent} {child}")).ToList();
        }

        return resolved;
    }

    private static IEnumerable<string> SplitSelectorList(string header)
    {
        var depth = 0;
        var current = new StringBuilder();

        foreach (var c in header)
        {
            if (c is '(' or '[') depth++;
            else if (c is ')' or ']') depth--;

            if (c == ',' && depth == 0)
            {
                yield return current.ToString().Trim();
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        yield return current.ToString().Trim();
    }

    private static string LastCompound(string selector)
    {
        var depth = 0;
        var start = 0;

        for (var i = 0; i < selector.Length; i++)
        {
            var c = selector[i];

            if (c is '(' or '[') depth++;
            else if (c is ')' or ']') depth--;
            else if (depth == 0 && (char.IsWhiteSpace(c) || c is '>' or '~' or '+')) start = i + 1;
        }

        return selector[start..];
    }

    private static IEnumerable<string> EnumerateComponentStylesheets()
        => new[] { "Bit.BlazorUI", "Bit.BlazorUI.Extras" }
            .SelectMany(project => Directory.EnumerateFiles(SourceFiles.GetDirectory(project, "Components"), "*.scss", SearchOption.AllDirectories));
}
