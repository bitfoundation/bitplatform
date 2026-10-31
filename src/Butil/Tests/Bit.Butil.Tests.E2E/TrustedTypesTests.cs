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
        // Sanitized in a template and then assigned to the table's innerHTML, the rows would survive as well - parsed
        // twice, in two contexts. Only a call to the table's own setHTML shows the markup was parsed once, in place.
        await Page.EvaluateAsync(
            """
            () => {
                const table = document.getElementById('tt-table');
                const setHTML = table.setHTML;
                window.ttSetHtmlCalls = 0;
                if (typeof setHTML !== 'function') return;
                table.setHTML = function (...args) {
                    window.ttSetHtmlCalls++;
                    return setHTML.apply(this, args);
                };
            }
            """);

        await ClickAndExpectAsync("tt-set-html", "tt:set:");
        var status = await CurrentStatusAsync();

        if (status == "tt:set:unsupported") Assert.Inconclusive("This browser has no Trusted Types or no Element.setHTML.");

        Assert.AreEqual("tt:set:True", status);
        Assert.AreEqual("<tbody><tr><td>x</td></tr></tbody>", await Page.Locator("#tt-table").InnerHTMLAsync());
        Assert.AreEqual(1, await Page.EvaluateAsync<int>("() => window.ttSetHtmlCalls"), "the markup must be written with the table's own setHTML");
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
