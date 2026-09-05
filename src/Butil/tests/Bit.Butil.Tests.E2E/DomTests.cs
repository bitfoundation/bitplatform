using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

[TestClass]
public class DomTests : ButilPageTest
{
    [TestMethod]
    public async Task Query_Finds_Elements_And_An_Invalid_Selector_Answers_Nothing()
    {
        // span / its text / the first child's tag / three children / an invalid selector gave null
        await ClickAndExpectAsync("dom-query", "dom:query:span/found me/span/3/True");
    }

    [TestMethod]
    public async Task A_Created_Element_Is_Not_Connected_Until_It_Is_Appended()
    {
        // before / after / the attribute survived the round trip / not connected once removed
        await ClickAndExpectAsync("dom-create", "dom:create:False/True/butil/False");
    }

    [TestMethod]
    public async Task Traversal_Is_Element_Wise_And_Skips_Text_Nodes()
    {
        await ClickAndExpectAsync("dom-traverse", "dom:traverse:div/b/True/div/True");
    }

    [TestMethod]
    public async Task A_Handle_Bridges_To_An_ElementReference_The_Extensions_Resolve()
    {
        // The canary for the one Blazor internal Butil leans on: a class added through an ordinary
        // ElementReference extension, read back through the Dom handle. A future Blazor that changes
        // how element references resolve fails here rather than in a consumer's app.
        await ClickAndExpectAsync("dom-bridge", "dom:bridge:bridged");
    }

    [TestMethod]
    public async Task SetText_Inserts_Characters_Rather_Than_Markup()
    {
        // The angle brackets come back escaped, and no element was created.
        await ClickAndExpectAsync("dom-text", "dom:text:&lt;b&gt;x&lt;/b&gt;/0");
    }
}
