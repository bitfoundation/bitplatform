using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace Bit.Bheadless.Tests.Testing;

/// <summary>The audit rules, accessible-name computation and focus order of Bit.Bheadless.Testing.</summary>
[TestClass]
public class A11yEngineTests
{
    private static readonly HtmlParser Parser = new();

    private static IElement Body(string html) => Parser.ParseDocument($"<body>{html}</body>").Body!;

    private static IElement El(string html, string selector) => Body(html).QuerySelector(selector)!;

    private static string[] Rules(string html, BitBhA11yOptions? options = null)
        => BitBhA11y.Audit(Body(html), options).Violations.Select(v => v.RuleId).Distinct().ToArray();

    [TestMethod]
    [DataRow("duplicate-id", "<p id='a'>x</p><p id='a'>y</p>")]
    [DataRow("aria-valid-role", "<div role='buton'>x</div>")]
    [DataRow("aria-valid-attr-value", "<button aria-expanded='yes'>x</button>")]
    [DataRow("aria-allowed-attr", "<div role='img' aria-label='x' aria-checked='true'></div>")]
    [DataRow("aria-required-attr", "<div role='slider' tabindex='0' aria-label='Volume'></div>")]
    [DataRow("idref-exists", "<input aria-labelledby='nope'>")]
    [DataRow("aria-required-children", "<div role='listbox' aria-label='x'><button>not an option</button></div>")]
    [DataRow("aria-required-parent", "<div role='option' aria-selected='false'>x</div>")]
    [DataRow("accessible-name", "<button></button>")]
    [DataRow("aria-hidden-focus", "<div aria-hidden='true'><button>x</button></div>")]
    [DataRow("nested-interactive", "<button>a <a href='/x'>b</a></button>")]
    [DataRow("tabindex", "<button tabindex='3'>x</button>")]
    [DataRow("presentation-conflict", "<button role='presentation'>x</button>")]
    [DataRow("activedescendant-target", "<div role='listbox' aria-label='x' tabindex='0' aria-activedescendant='o'></div><div role='listbox' aria-label='y'><div role='option' id='o' aria-selected='false'>x</div></div>")]
    [DataRow("form-label", "<input type='text'>")]
    public void Rule_reports_its_violation(string rule, string html)
        => Assert.Contains(rule, Rules(html));

    [TestMethod]
    [DataRow("<button>Save</button>")]
    [DataRow("<label for='e'>Email</label><input id='e' type='email'>")]
    [DataRow("<label>Name <input></label>")]
    [DataRow("<div role='listbox' aria-label='Fruit' tabindex='0' aria-activedescendant='o1'><div role='option' id='o1' aria-selected='true'>Apple</div></div>")]
    [DataRow("<ul role='tree' aria-label='Files'><li role='treeitem' aria-expanded='false' tabindex='0'>src<ul role='group'><li role='treeitem' tabindex='-1'>a</li></ul></li></ul>")]
    [DataRow("<div aria-hidden='true'><button tabindex='-1'>x</button></div>")]
    [DataRow("<div role='slider' tabindex='0' aria-label='Volume' aria-valuenow='5' aria-valuemin='0' aria-valuemax='10'></div>")]
    [DataRow("<img src='a.png' alt=''>")]
    public void Valid_markup_passes(string html)
    {
        var report = BitBhA11y.Audit(Body(html));
        Assert.IsTrue(report.Passed, report.ToString());
    }

    [TestMethod]
    public void Fragments_ignore_references_outside_the_markup()
    {
        const string html = "<input aria-describedby='page-help' aria-label='x'>";
        Assert.Contains("idref-exists", Rules(html));
        Assert.DoesNotContain("idref-exists", Rules(html, new BitBhA11yOptions { IsFragment = true }));
    }

    [TestMethod]
    public void Rules_can_be_disabled_and_filtered_by_impact()
    {
        const string html = "<button></button><button tabindex='2'>x</button>";
        Assert.DoesNotContain("accessible-name", Rules(html, new BitBhA11yOptions().Without("accessible-name")));
        var serious = BitBhA11y.Audit(Body(html), new BitBhA11yOptions { MinimumImpact = BitBhA11yImpact.Critical });
        foreach (var v in serious.Violations) Assert.AreEqual(BitBhA11yImpact.Critical, v.Impact);
    }

    [TestMethod]
    public void Report_renders_for_ci()
    {
        var report = BitBhA11y.Audit(Body("<button></button>"));
        Assert.IsFalse(report.Passed);
        Assert.Contains("accessible-name", report.ToMarkdown("Audit"));
        Assert.Contains("\"accessible-name\"", report.ToJson());
        var ex = new BitBhA11yException(report);
        Assert.Contains("accessible-name", ex.Message);
    }

    // ── Accessible name (accname 1.2) ────────────────────────────────────────

    [TestMethod]
    [DataRow("<button id='t'>Save</button>", "Save")]
    [DataRow("<button id='t' aria-label='Close'>×</button>", "Close")]
    [DataRow("<span id='l1'>Billing</span><span id='l2'>Address</span><div id='t' role='group' aria-labelledby='l1 l2'></div>", "Billing Address")]
    [DataRow("<label for='t'>Email</label><input id='t'>", "Email")]
    [DataRow("<label>Remember me <input id='t' type='checkbox'></label>", "Remember me")]
    [DataRow("<button id='t'><img src='i.png' alt='Print'> now</button>", "Print now")]
    [DataRow("<button id='t'>Delete <span aria-hidden='true'>🗑</span></button>", "Delete")]
    [DataRow("<input id='t' title='Search'>", "Search")]
    [DataRow("<input id='t' placeholder='Search…'>", "Search…")]
    [DataRow("<a id='t' href='/'>Home <span hidden>secret</span></a>", "Home")]
    [DataRow("<button id='t' aria-labelledby='t x'>Ok</button><span id='x'>dialog</span>", "Ok dialog")]
    [DataRow("<ul role='tree'><li id='t' role='treeitem'><span>src</span><ul role='group'><li role='treeitem'>app.cs</li></ul></li></ul>", "src")]
    public void Accessible_name(string html, string expected)
        => Assert.AreEqual(expected, El(html, "#t").AccessibleName());

    [TestMethod]
    public void Accessible_description_follows_describedby_then_title()
    {
        var body = Body("<p id='d'>Must be 8+ chars.</p><input id='a' aria-label='Password' aria-describedby='d' title='ignored'><input id='b' aria-label='Code' title='Six digits'>");
        Assert.AreEqual("Must be 8+ chars.", body.QuerySelector("#a")!.AccessibleDescription());
        Assert.AreEqual("Six digits", body.QuerySelector("#b")!.AccessibleDescription());
    }

    // ── Focus order ──────────────────────────────────────────────────────────

    [TestMethod]
    public void Tab_sequence_puts_positive_tabindex_first_and_skips_unfocusable()
    {
        var body = Body(
            "<a href='/a'>A</a>" +
            "<button disabled>Disabled</button>" +
            "<button tabindex='-1'>Programmatic</button>" +
            "<div hidden><button>Hidden</button></div>" +
            "<div inert><button>Inert</button></div>" +
            "<input type='hidden'>" +
            "<button tabindex='1'>First</button>" +
            "<div tabindex='0' role='button'>Div</div>" +
            "<input type='radio' name='r' checked aria-label='R1'><input type='radio' name='r' aria-label='R2'>");

        var names = BitBhFocusOrder.TabSequence(body).Select(s => s.Name).ToArray();
        Assert.AreSequenceEqual(new[] { "First", "A", "Div", "R1" }, names);
    }

    [TestMethod]
    public void Tab_stop_describes_role_and_name()
    {
        var stops = BitBhFocusOrder.TabSequence(Body("<button>Save</button>"));
        Assert.AreEqual("button \"Save\"", stops[0].ToString());
        Assert.Contains("1. button \"Save\"", BitBhFocusOrder.Describe(stops));
    }
}
