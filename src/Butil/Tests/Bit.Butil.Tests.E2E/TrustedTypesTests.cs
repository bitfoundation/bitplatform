using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

/// <summary>
/// How a sanitizing Trusted Types policy writes into an element, driven from JS so the target can be
/// an element whose parsing context differs from a plain <c>div</c>.
/// </summary>
[TestClass]
public class TrustedTypesTests : ButilPageTest
{
    [TestMethod]
    public async Task A_Sanitizing_Policy_Parses_Markup_In_The_Target_Elements_Own_Context()
    {
        // Table rows mean nothing inside a div: sanitized there first, the rows are dropped and only
        // "x" is left to write into the table. Parsed in the table's own context they survive - and
        // the handler and the script are still removed, so sanitizing happened in that same pass.
        var result = await Page.EvaluateAsync<string>(
            """
            () => {
                if (typeof window.trustedTypes?.createPolicy !== 'function' || typeof Element.prototype.setHTML !== 'function') return 'unsupported';
                const name = 'butil-e2e-in-context';
                if (BitButil.trustedTypes.createPolicy(name, {}, null) !== true) return 'no policy';
                const table = document.createElement('table');
                const written = BitButil.trustedTypes.setHtml(table, name, '<tr><td onclick="alert(1)">x</td></tr><script>alert(2)<\/script>');
                return `${written}|${table.innerHTML}`;
            }
            """);

        if (result == "unsupported") Assert.Inconclusive("This browser has no Trusted Types or no Element.setHTML.");

        Assert.AreEqual("true|<tbody><tr><td>x</td></tr></tbody>", result);
    }
}
