using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

[TestClass]
public class CssTests : ButilPageTest
{
    [TestMethod]
    public async Task Computed_Style_Reports_Resolved_Values()
    {
        await ClickAndExpectAsync("css-computed", "css:computed:rgb(255, 0, 0)/20px/20px");
    }

    [TestMethod]
    public async Task Supports_Asks_The_Parser_And_Escape_Makes_A_Selector_Safe()
    {
        // grid yes / nonsense no / the same as a condition / a dot escaped for use in a selector
        await ClickAndExpectAsync("css-supports", @"css:supports:True/False/True/a\.b");
    }

    [TestMethod]
    public async Task A_Rule_Applies_Until_It_Is_Deleted_And_A_Bad_One_Is_Refused()
    {
        // inserted at 0 / the rule took effect / the malformed one answered -1 / one rule in the
        // sheet / and the value is back to normal once deleted
        await ClickAndExpectAsync("css-sheet", "css:sheet:0/3px/-1/1/normal");
    }

    [TestMethod]
    public async Task Highlighting_Finds_Every_Occurrence_Without_Touching_The_Dom()
    {
        await ClickAndExpectAsync("css-highlight", "css:highlight:2");
    }
}
