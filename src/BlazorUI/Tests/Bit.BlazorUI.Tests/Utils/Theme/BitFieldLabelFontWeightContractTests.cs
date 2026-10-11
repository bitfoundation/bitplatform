using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// Pins that every caption naming a form field takes its weight from the theme's field-label token
/// (<c>$tg-field-label-font-weight</c>), the one BitLabel reads too.
/// </summary>
/// <remarks>
/// Fluent captions a field semibold, while Fluent 2, Material and Cupertino caption it regular, so a caption that
/// reads the semibold step itself stays semibold under those presets beside every other input and BitLabel. Reading
/// the token through the theme also keeps a caption following a <c>FontWeights</c> override of a BitThemeProvider,
/// since the alias is re-declared for the overlay (the <c>typography</c> group). A new input's caption goes in the
/// list below; the sweep catches one that missed the token and is still written as the Fluent semibold.
/// </remarks>
[TestClass]
public sealed class BitFieldLabelFontWeightContractTests
{
    private static readonly Regex FieldLabelWeight = new(
        @"font-weight:[^;]*\$tg-field-label-font-weight",
        RegexOptions.Compiled);

    private static readonly Regex SemiboldWeight = new(
        @"font-weight:[^;]*\$tg-fw-semibold",
        RegexOptions.Compiled);

    // The caption of a component, as its stylesheet names it: .bit-<prefix>-lbl.
    private static readonly Regex CaptionSelector = new(
        @"^\.bit-[a-z]+-lbl$",
        RegexOptions.Compiled);

    [TestMethod,
        DataRow("Bit.BlazorUI/Components/Inputs/ChoiceGroup/BitChoiceGroup.scss", ".bit-chg-lbl"),
        DataRow("Bit.BlazorUI/Components/Inputs/CircularTimePicker/BitCircularTimePicker.scss", ".bit-ctp-lbl"),
        DataRow("Bit.BlazorUI/Components/Inputs/ColorPicker/BitColorPicker.scss", ".bit-clp-lbl"),
        DataRow("Bit.BlazorUI/Components/Inputs/DatePicker/BitDatePicker.scss", ".bit-dtp-lbl"),
        DataRow("Bit.BlazorUI/Components/Inputs/DateRangePicker/BitDateRangePicker.scss", ".bit-dtrp-lbl"),
        DataRow("Bit.BlazorUI/Components/Inputs/Dropdown/BitDropdown.scss", ".bit-drp-lbl"),
        DataRow("Bit.BlazorUI/Components/Inputs/NumberField/BitNumberField.scss", ".bit-nfl-lbl"),
        DataRow("Bit.BlazorUI/Components/Inputs/OtpInput/BitOtpInput.scss", ".bit-otp-lbl"),
        DataRow("Bit.BlazorUI/Components/Inputs/Rating/BitRating.scss", ".bit-rtg-lbl"),
        DataRow("Bit.BlazorUI/Components/Inputs/SearchBox/BitSearchBox.scss", ".bit-srb-lbl"),
        DataRow("Bit.BlazorUI/Components/Inputs/Slider/BitSlider.scss", ".bit-sld-lbl"),
        DataRow("Bit.BlazorUI/Components/Inputs/TagsInput/BitTagsInput.scss", ".bit-tgi-lbl"),
        DataRow("Bit.BlazorUI/Components/Inputs/TextField/BitTextField.scss", ".bit-tfl-lbl"),
        DataRow("Bit.BlazorUI/Components/Inputs/TimePicker/BitTimePicker.scss", ".bit-tpc-lbl"),
        DataRow("Bit.BlazorUI/Components/Inputs/Toggle/BitToggle.scss", ".bit-tgl-lbl"),
        DataRow("Bit.BlazorUI.Extras/Components/FullCalendar/BitFullCalendar.scss", ".bit-bfc-field-label"),
        DataRow("Bit.BlazorUI.Extras/Components/MarkdownEditor/BitMarkdownEditor.scss", ".bit-mde-lbl"),
        DataRow("Bit.BlazorUI.Extras/Components/PhoneInput/BitPhoneInput.scss", ".bit-phi-lbl"),
        DataRow("Bit.BlazorUI.Extras/Components/PhoneInput/BitPhoneInput.scss", ".bit-phi-rlb"),
        DataRow("Bit.BlazorUI.Extras/Components/RichTextEditor/BitRichTextEditor.scss", ".bit-rte-lbl")]
    public void FieldCaptionShouldTakeItsWeightFromTheFieldLabelToken(string path, string selector)
    {
        var stylesheet = SourceFiles.Read(path.Split('/'));

        var declarations = SourceFiles.GetScssDeclarations(stylesheet, $"\n{selector} {{", $"{path} has no {selector} rule.");

        Assert.IsTrue(FieldLabelWeight.IsMatch(declarations), $"{selector} of {path} does not read $tg-field-label-font-weight.");
        Assert.IsFalse(SemiboldWeight.IsMatch(declarations), $"{selector} of {path} still reads $tg-fw-semibold.");
    }

    [TestMethod]
    public void NoComponentCaptionShouldReadTheSemiboldStep()
    {
        var offenders = new List<string>();

        foreach (var file in EnumerateComponentStylesheets())
        {
            var stylesheet = SourceFiles.StripScssComments(SourceFiles.ReadFullPath(file));

            foreach (var rule in SourceFiles.GetScssRules(stylesheet))
            {
                if (rule.Ancestors.Count > 0 || CaptionSelector.IsMatch(rule.Header) is false) continue;

                var declarations = SourceFiles.GetScssDeclarations(stylesheet[rule.Index..], rule.Header);

                if (SemiboldWeight.IsMatch(declarations))
                {
                    offenders.Add($"{Path.GetRelativePath(SourceFiles.Root, file)}: {rule.Header}");
                }
            }
        }

        Assert.AreEqual(0, offenders.Count,
            "These captions read $tg-fw-semibold rather than $tg-field-label-font-weight:\n" + string.Join("\n", offenders));
    }

    private static IEnumerable<string> EnumerateComponentStylesheets()
        => new[] { "Bit.BlazorUI", "Bit.BlazorUI.Extras" }
            .SelectMany(project => Directory.EnumerateFiles(SourceFiles.GetDirectory(project, "Components"), "*.scss", SearchOption.AllDirectories));
}
