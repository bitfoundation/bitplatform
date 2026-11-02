using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.DataGrid;

/// <summary>
/// Pins the public CSS variables of the grid, which a bUnit render cannot see: each one is read with a fallback and
/// never declared, is listed in the header of the stylesheet and in the table of the demo page.
/// </summary>
[TestClass]
public class BitDataGridStylesheetTests
{
    private static readonly string[] PublicVariables =
    [
        "--bit-DataGrid-background",
        "--bit-DataGrid-color",
        "--bit-DataGrid-font-family",
        "--bit-DataGrid-font-size",
        "--bit-DataGrid-line-height",
        "--bit-DataGrid-border-color",
        "--bit-DataGrid-border-radius",
        "--bit-DataGrid-cell-padding",
        "--bit-DataGrid-header-background",
        "--bit-DataGrid-header-color",
        "--bit-DataGrid-header-font-weight",
        "--bit-DataGrid-stripe-background",
        "--bit-DataGrid-hover-background",
        "--bit-DataGrid-selected-background",
        "--bit-DataGrid-selected-color",
        "--bit-DataGrid-editing-background",
        "--bit-DataGrid-accent-color",
        "--bit-DataGrid-focus-color",
        "--bit-DataGrid-group-background",
        "--bit-DataGrid-group-indent",
        "--bit-DataGrid-tree-indent",
        "--bit-DataGrid-detail-background",
        "--bit-DataGrid-footer-background",
        "--bit-DataGrid-disabled-color",
        "--bit-DataGrid-placeholder-color",
    ];

    [TestMethod]
    public void BitDataGridShouldReadEveryPublicVariableWithoutDeclaringIt()
    {
        var stylesheet = SourceFiles.ReadStylesheet("Bit.BlazorUI.Extras", "Components", "DataGrid", "BitDataGrid.scss");

        var read = Regex.Matches(stylesheet, @"var\((--bit-DataGrid-[a-z-]+)").Select(m => m.Groups[1].Value).Distinct().ToArray();

        CollectionAssert.AreEquivalent(PublicVariables, read);

        foreach (var variable in PublicVariables)
        {
            Assert.IsFalse(Regex.IsMatch(stylesheet, $@"^\s*{variable}\s*:", RegexOptions.Multiline), $"{variable} is declared, so it no longer inherits.");
            StringAssert.Contains(stylesheet, $"//   {variable} ", $"{variable} is missing from the header of the stylesheet.");
        }
    }

    [TestMethod]
    public void BitDataGridShouldListEveryPublicVariableOnItsDemoPage()
    {
        var demo = SourceFiles.Read("Demo", "Client", "Bit.BlazorUI.Demo.Client.Core", "Pages", "Components", "Extras", "DataGrid", "BitDataGridDemo.razor.params.cs");

        var listed = Regex.Matches(demo, @"Name = ""(--bit-DataGrid-[a-z-]+)""").Select(m => m.Groups[1].Value).ToArray();

        CollectionAssert.AreEquivalent(PublicVariables, listed);
    }

    [TestMethod]
    public void BitDataGridShouldResetItsRowStateOnEveryRoot()
    {
        var stylesheet = SourceFiles.ReadStylesheet("Bit.BlazorUI.Extras", "Components", "DataGrid", "BitDataGrid.scss");

        var root = SourceFiles.GetScssBlock(stylesheet, "\n.bit-dtg {");

        // A grid nested in another one's template must not inherit the state of the row it sits in.
        foreach (var property in new[] { "--bit-dtg-rbg", "--bit-dtg-cbg", "--bit-dtg-tint", "--bit-dtg-stripe-bg", "--bit-dtg-hover-bg" })
        {
            StringAssert.Contains(root, $"{property}: initial;");
        }
    }

    [TestMethod]
    public void BitDataGridShouldGiveEveryControlAFocusIndicator()
    {
        var stylesheet = SourceFiles.ReadStylesheet("Bit.BlazorUI.Extras", "Components", "DataGrid", "BitDataGrid.scss");

        // The library resets `button { outline: none }`, so each button family draws its own indicator.
        foreach (var selector in new[] { "button.bit-dtg-htext", ".bit-dtg-btn", ".bit-dtg-icon-btn", ".bit-dtg-drag-handle" })
        {
            var block = SourceFiles.GetScssBlock(stylesheet, $"\n{selector} {{");
            StringAssert.Contains(block, "&:focus-visible", selector);
        }

        StringAssert.Contains(stylesheet, ".bit-dtg-resizer:focus-visible");
        StringAssert.Contains(stylesheet, "@media (forced-colors: active)");
    }

    // The hint of an unsorted header is laid out like the arrow it previews, holding its room while hidden. With no room
    // of its own it would be clipped by a label its title filled, leave a fitted column no room for the arrow, spill
    // over the group toggle and the cell's padding, and the first sort would reflow the header.
    [TestMethod]
    public void BitDataGridShouldLetTheSortHintHoldTheRoomOfTheArrow()
    {
        var stylesheet = SourceFiles.ReadStylesheet("Bit.BlazorUI.Extras", "Components", "DataGrid", "BitDataGrid.scss");

        var hint = SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-dtg-sortable:not([aria-sort]) > .bit-dtg-htext::after {");
        StringAssert.Contains(hint, "font-size: $tg-fs-2xs;");
        StringAssert.Contains(hint, "opacity: 0;");
        foreach (var property in new[] { "width", "margin", "padding", "position", "display" })
        {
            Assert.IsFalse(Regex.IsMatch(hint, $@"(^|[\s;{{]){property}(-[a-z-]+)?\s*:"), $"The hint sets {property}, which would lay it out unlike the arrow it previews.");
        }
    }

    // A wrapped header breaks a word only once it alone is wider than its line, and a HeaderTemplate's content inherits
    // that rule rather than undoing another: anywhere let a title laid out beside an icon break in the middle of a word
    // to save its flex row a few pixels (#13963). The title the grid writes shrinks to its room as it always has, and an
    // element of a template is held to the label's width, so a word wider than the column still breaks inside it.
    [TestMethod]
    public void BitDataGridShouldKeepTheWordsOfAWrappedHeaderWholeUnlessWiderThanTheColumn()
    {
        var stylesheet = SourceFiles.ReadStylesheet("Bit.BlazorUI.Extras", "Components", "DataGrid", "BitDataGrid.scss");

        var label = SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-dtg-hcell-wrap .bit-dtg-htext {");
        StringAssert.Contains(label, "overflow-wrap: break-word;");
        Assert.IsFalse(label.Contains("anywhere"), "A template's content would inherit anywhere.");

        StringAssert.Contains(SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-dtg-hcell-wrap .bit-dtg-htitle {"), "min-width: 0;");
        StringAssert.Contains(SourceFiles.GetScssDeclarations(stylesheet, "\n:where(.bit-dtg-hcell-wrap .bit-dtg-htext > *) {"), "max-width: 100%;");
    }

    // The primary button's label is the on-color of its fill, Canvas under a forced palette, which Chromium's Canvas
    // backplate behind text hides; it is painted as a native button there instead.
    [TestMethod]
    public void BitDataGridShouldPaintThePrimaryButtonAsANativeOneUnderForcedColors()
    {
        var stylesheet = SourceFiles.ReadStylesheet("Bit.BlazorUI.Extras", "Components", "DataGrid", "BitDataGrid.scss");

        var forced = SourceFiles.GetScssBlock(stylesheet, "\n@media (forced-colors: active) {");

        StringAssert.Contains(forced, """
                .bit-dtg-btn-primary:not(:disabled) {
                    color: ButtonText;
                    border-color: ButtonText;
                    background-color: ButtonFace;
                }
            """.Replace("\r\n", "\n"));
    }
}
