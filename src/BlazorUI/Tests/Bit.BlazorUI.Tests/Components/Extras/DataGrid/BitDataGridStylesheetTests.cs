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

    // The hint of an unsorted header hangs past its title instead of taking room of its own: laid out like the arrow it
    // previews, it narrowed every sortable title by its width and the gap even while invisible, so a wrapped title
    // broke before it had to (#13963).
    [TestMethod]
    public void BitDataGridShouldLetTheSortHintTakeNoRoomOfItsOwn()
    {
        var stylesheet = SourceFiles.ReadStylesheet("Bit.BlazorUI.Extras", "Components", "DataGrid", "BitDataGrid.scss");

        StringAssert.Contains(SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-dtg-htext {"), "gap: $dtg-htext-gap;");

        var hint = SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-dtg-sortable:not([aria-sort]) > .bit-dtg-htext::after {");
        StringAssert.Contains(hint, "width: 0;");
        StringAssert.Contains(hint, "padding-inline-start: $dtg-htext-gap;");
        StringAssert.Contains(hint, "margin-inline: calc(-1 * #{$dtg-htext-gap});");
        Assert.IsFalse(hint.Contains("display: none"), "Hidden from the layout, the hint would move the title as it shows.");
    }

    // A wrapped title the grid writes may still break a word wider than the room its column gives it, but the elements
    // of a HeaderTemplate keep their words whole: anywhere let a title laid out beside an icon break in the middle of a
    // word to save its flex row a few pixels (#13963).
    [TestMethod]
    public void BitDataGridShouldKeepTheWordsOfAWrappedHeaderTemplateWhole()
    {
        var stylesheet = SourceFiles.ReadStylesheet("Bit.BlazorUI.Extras", "Components", "DataGrid", "BitDataGrid.scss");

        StringAssert.Contains(SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-dtg-hcell-wrap .bit-dtg-htext {"), "overflow-wrap: anywhere;");
        StringAssert.Contains(SourceFiles.GetScssDeclarations(stylesheet, "\n:where(.bit-dtg-hcell-wrap .bit-dtg-htext > *) {"), "overflow-wrap: break-word;");
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
