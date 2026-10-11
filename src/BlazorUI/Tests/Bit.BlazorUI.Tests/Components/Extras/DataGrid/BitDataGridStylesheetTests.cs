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
}
