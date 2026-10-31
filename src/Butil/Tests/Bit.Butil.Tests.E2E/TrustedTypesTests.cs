using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

/// <summary>
/// How a sanitizing Trusted Types policy parses markup: in the target element's own context where it has one, and
/// where it has none, in a way the sink's parse cannot change the meaning of.
/// </summary>
[TestClass]
public class TrustedTypesTests : ButilPageTest
{
    [TestMethod]
    public async Task A_Sanitizing_Policy_Parses_Markup_In_The_Target_Elements_Own_Context()
    {
        await ClickAndExpectAsync("tt-set-html", "tt:set:");
        var status = await CurrentStatusAsync();

        if (status == "tt:set:unsupported") Assert.Inconclusive("This browser has no Trusted Types or no Element.setHTML.");

        Assert.AreEqual("tt:set:True", status);
        Assert.AreEqual("<tbody><tr><td>x</td></tr></tbody>", await Page.Locator("#tt-table").InnerHTMLAsync());
    }

    [TestMethod]
    public async Task A_Sanitizing_Policy_Hands_A_Sink_Only_Markup_That_Parses_The_Same_Anywhere()
    {
        await ClickAndExpectAsync("tt-create-html", "tt:create:");
        var status = await CurrentStatusAsync();

        if (status == "tt:create:unsupported") Assert.Inconclusive("This browser has no Trusted Types or no configurable Sanitizer.");

        // Table rows survive, being sanitized in a template's context rather than a div's. A '<' left in a style
        // element's text is refused - inside an SVG element it would parse as the img it spells - while a style
        // sheet without one comes back as it went in.
        Assert.AreEqual("tt:create:<tr><td>x</td></tr>|null|<style>p > b { color: red }</style><p>ok</p>", status);
    }
}
