using System.Globalization;
using System.Text.Json;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace Bit.Bheadless.Tests.Testing;

/// <summary>Role computation (HTML-AAM / WAI-ARIA 1.2), the audit rules and the report of Bit.Bheadless.Testing.</summary>
[TestClass]
public class AriaRuleTests
{
    private static readonly HtmlParser Parser = new();

    private static IElement Body(string html) => Parser.ParseDocument($"<body>{html}</body>").Body!;

    private static IElement El(string html) => Body(html).QuerySelector("#t")!;

    private static BitBhA11yReport Audit(string html) => BitBhA11y.Audit(Body(html));

    private static string[] Rules(string html) => Audit(html).Violations.Select(v => v.RuleId).Distinct().ToArray();

    // ── Roles ────────────────────────────────────────────────────────────────

    [TestMethod]
    [DataRow("<table role='grid' aria-label='Files'><tr><td id='t' aria-selected='true' aria-readonly='true'>a</td></tr></table>", "gridcell")]
    [DataRow("<table role='treegrid' aria-label='Files'><tr><td id='t' aria-expanded='false'>a</td></tr></table>", "gridcell")]
    [DataRow("<table aria-label='Files'><tr><td id='t'>a</td></tr></table>", "cell")]
    [DataRow("<table role='grid' aria-label='Files'><thead><tr><th id='t' aria-sort='ascending'>Name</th></tr></thead><tr><td>a</td></tr></table>", "columnheader")]
    [DataRow("<table aria-label='Files'><tr><th id='t'>Name</th><td>a</td></tr></table>", "rowheader")]
    [DataRow("<table aria-label='Files'><tr><th id='t' scope='col'>Name</th><td>a</td></tr></table>", "columnheader")]
    [DataRow("<table aria-label='Files'><tr><th id='t'>Name</th><th>Size</th></tr><tr><td>a</td><td>1</td></tr></table>", "columnheader")]
    public void Table_cells_follow_their_table(string html, string role)
    {
        var body = Body(html);
        Assert.AreEqual(role, body.QuerySelector("#t")!.Role());
        var report = BitBhA11y.Audit(body);
        Assert.IsTrue(report.Passed, report.ToString());
    }

    [TestMethod]
    [DataRow("<header id='t'>x</header>", "banner")]
    [DataRow("<article><header id='t'>x</header></article>", "")]
    [DataRow("<main><header id='t'>x</header></main>", "")]
    [DataRow("<div role='navigation'><footer id='t'>x</footer></div>", "")]
    [DataRow("<footer id='t'>x</footer>", "contentinfo")]
    [DataRow("<aside id='t'>x</aside>", "complementary")]
    [DataRow("<main><aside id='t'>x</aside></main>", "complementary")]
    [DataRow("<section><aside id='t'>x</aside></section>", "")]
    [DataRow("<section><aside id='t' aria-label='Related'>x</aside></section>", "complementary")]
    [DataRow("<form id='t'></form>", "")]
    [DataRow("<form id='t' aria-label='Search'></form>", "form")]
    [DataRow("<section id='t' title='News'></section>", "region")]
    public void Landmarks_depend_on_scope_and_name(string html, string role)
        => Assert.AreEqual(role.Length == 0 ? null : role, El(html).Role());

    [TestMethod]
    [DataRow("password")]
    [DataRow("date")]
    [DataRow("time")]
    [DataRow("datetime-local")]
    [DataRow("month")]
    [DataRow("week")]
    [DataRow("color")]
    [DataRow("file")]
    public void Input_types_without_an_aria_role(string type)
    {
        Assert.IsNull(El($"<input id='t' type='{type}'>").Role());
        // Still a form field: it needs a label.
        Assert.Contains("form-label", Rules($"<input type='{type}'>"));
        Assert.IsTrue(Audit($"<label>Field <input type='{type}'></label>").Passed);
    }

    [TestMethod]
    [DataRow("text", "textbox")]
    [DataRow("email", "textbox")]
    [DataRow("unknown", "textbox")]
    [DataRow("search", "searchbox")]
    [DataRow("number", "spinbutton")]
    [DataRow("range", "slider")]
    [DataRow("image", "button")]
    public void Input_types_with_an_aria_role(string type, string role)
        => Assert.AreEqual(role, El($"<input id='t' type='{type}'>").Role());

    [TestMethod]
    public void Roles_follow_presentational_rules_and_fallback_lists()
    {
        Assert.AreEqual("none", El("<span id='t' role='none presentation'>x</span>").Role());
        Assert.AreEqual("button", El("<span id='t' role='made-up BUTTON'>x</span>").Role());
        // Presentational role conflict resolution: focusable or globally ARIA-attributed elements keep their native role.
        Assert.AreEqual("button", El("<button id='t' role='presentation'>x</button>").Role());
        Assert.IsNull(El("<div id='t' role='none' aria-label='x'>y</div>").Role());
        Assert.AreEqual("img", El("<img id='t' src='a.png' alt='' aria-label='Logo'>").Role());
        // Required owned elements of a presentational list / table inherit the role.
        Assert.AreEqual("none", El("<ul role='none'><li id='t'>a</li></ul>").Role());
        Assert.AreEqual("listitem", El("<ul role='none'><li id='t' tabindex='0'>a</li></ul>").Role());
        Assert.AreEqual("presentation", El("<table role='presentation'><tr><td id='t'>a</td></tr></table>").Role());
        Assert.AreEqual("presentation", El("<table role='presentation'><tr id='t'><td>a</td></tr></table>").Role());
        // Only a details element's first summary child is its disclosure button.
        Assert.AreEqual("button", El("<details><summary id='t'>More</summary></details>").Role());
        Assert.IsNull(El("<details><summary>More</summary><summary id='t'>Again</summary></details>").Role());
    }

    [TestMethod]
    public void Datalist_is_never_rendered()
        => Assert.IsTrue(BitBhAria.IsHidden(El("<datalist id='t'><option value='a'></option></datalist>")));

    [TestMethod]
    public void Rule_tables_are_read_only()
    {
        Assert.ThrowsExactly<NotSupportedException>(() => ((ICollection<string>)BitBhAria.ValidRoles).Add("made-up"));
        Assert.ThrowsExactly<NotSupportedException>(() => ((IList<string>)BitBhAria.RoleSpecificAttributes["aria-valuenow"])[0] = "button");
        Assert.ThrowsExactly<NotSupportedException>(() => ((IDictionary<string, IReadOnlyList<string>>)BitBhAria.RequiredOwned).Remove("list"));
        Assert.Contains("toolbar", BitBhAria.RoleSpecificAttributes["aria-activedescendant"]);
    }

    // ── Rules ────────────────────────────────────────────────────────────────

    [TestMethod]
    [DataRow("<ul role='none'><li>a</li></ul>")]
    [DataRow("<ul role='none presentation'><li>a</li></ul>")]
    [DataRow("<table role='presentation'><tr><td>a</td><th>b</th></tr></table>")]
    [DataRow("<datalist id='d'><option value='a'></option></datalist><input list='d' aria-label='Fruit'>")]
    [DataRow("<input type='password' aria-label='Password'>")]
    [DataRow("<div role='toolbar' aria-label='Format' tabindex='0' aria-activedescendant='b'><button id='b' tabindex='-1'>Bold</button></div>")]
    [DataRow("<button aria-expanded='True' aria-pressed='FALSE'>x</button>")]
    [DataRow("<div role='doc-chapter'>x</div><a role='doc-noteref' href='#n'>1</a>")]
    [DataRow("<div role='list' aria-owns='i'></div><div id='i' role='listitem'>x</div>")]
    [DataRow("<div role='separator'></div><hr>")]
    [DataRow("<div role='menu' aria-label='Edit'><div role='group' aria-label='Clipboard'><div role='menuitem'>Cut</div><div role='separator'></div></div></div>")]
    [DataRow("<div role='tree' aria-label='Files'><div role='treeitem' aria-selected='false'>src<div role='group'><div role='treeitem' aria-selected='false'>a</div></div></div></div>")]
    [DataRow("<div aria-hidden='true' hidden><button>x</button></div>")]
    [DataRow("<div aria-hidden='true' style='display: none'><button>x</button></div>")]
    [DataRow("<div aria-hidden='true' inert><button>x</button></div>")]
    [DataRow("<div aria-hidden='true'><fieldset disabled><button>x</button></fieldset></div>")]
    [DataRow("<div aria-hidden='true'><div tabindex='abc'>x</div></div>")]
    public void Valid_markup_passes(string html)
    {
        var report = Audit(html);
        Assert.IsTrue(report.Passed, report.ToString());
    }

    [TestMethod]
    [DataRow("aria-required-attr", "<div role='meter' aria-label='Disk'></div>")]
    [DataRow("aria-required-attr", "<div role='separator' tabindex='0' aria-label='Resize'></div>")]
    [DataRow("aria-required-attr", "<div role='checkbox' aria-checked='' tabindex='0'>x</div>")]
    [DataRow("accessible-name", "<h2></h2>")]
    [DataRow("accessible-name", "<div role='tooltip'></div>")]
    [DataRow("accessible-name", "<table aria-label='t'><tr><th></th></tr><tr><td>a</td></tr></table>")]
    [DataRow("accessible-name", "<button role='presentation'></button>")]
    [DataRow("form-label", "<input type='image' src='go.png'>")]
    [DataRow("aria-required-children", "<div role='listbox' aria-label='x'><div role='group' aria-label='g'><div role='menuitem'>a</div></div></div>")]
    [DataRow("aria-required-children", "<div role='tree' aria-label='x'><div role='treeitem' aria-selected='false'>a<div role='group'><div role='group'><div role='treeitem'>b</div></div></div></div></div>")]
    [DataRow("aria-required-children", "<div role='list' aria-owns='o'></div><button id='o'>x</button>")]
    [DataRow("aria-required-parent", "<div role='menu' aria-label='m' aria-owns='i'></div><div role='list'><div role='listitem' id='i'>x</div></div>")]
    [DataRow("presentation-conflict", "<div role='none presentation' aria-label='x'>y</div>")]
    [DataRow("aria-valid-attr-value", "<div role='meter' aria-label='Disk' aria-valuenow='150'></div>")]
    public void Rule_reports_its_violation(string rule, string html)
        => Assert.Contains(rule, Rules(html));

    [TestMethod]
    public void Aria_owns_cycles_are_ignored()
    {
        // Each claims the other: the second claim would close a cycle, so only the first counts.
        var report = Audit("<div role='list' id='a' aria-owns='b'></div><div role='listitem' id='b' aria-owns='a'>x</div>");
        Assert.IsTrue(report.Passed, report.ToString());
    }

    [TestMethod]
    public void Token_values_are_case_insensitive_and_empty_ones_minor()
    {
        Assert.IsTrue(Audit("<button aria-expanded='True'>x</button>").Passed);
        var empty = Assert.ContainsSingle(Audit("<button aria-expanded=''>x</button>").Violations);
        Assert.AreEqual(BitBhA11yImpact.Minor, empty.Impact);
        Assert.AreEqual("aria-valid-attr-value", empty.RuleId);
    }

    [TestMethod]
    public void Range_messages_use_aria_defaults_and_invariant_numbers()
    {
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
        try
        {
            static string Message(string html) => Audit(html).Violations.Single(v => v.RuleId == "aria-valid-attr-value").Message;
            Assert.AreEqual("aria-valuenow (100.5) is above the default aria-valuemax (100).", Message("<div role='meter' aria-label='Disk' aria-valuenow='100.5'></div>"));
            Assert.AreEqual("aria-valuenow (1.5) is below aria-valuemin (2.5).", Message("<div role='spinbutton' aria-label='Qty' tabindex='0' aria-valuenow='1.5' aria-valuemin='2.5'></div>"));
            // A spinbutton has no default bounds.
            Assert.IsTrue(Audit("<div role='spinbutton' aria-label='Qty' tabindex='0' aria-valuenow='-1000000'></div>").Passed);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [TestMethod]
    public void Aria_hidden_focus_reports_each_element_once()
    {
        var report = Audit("<div aria-hidden='true'><div aria-hidden='true'><button>x</button></div></div>");
        Assert.AreEqual(1, report.Violations.Count(v => v.RuleId == "aria-hidden-focus"));
    }

    [TestMethod]
    public void Duplicate_ids_report_every_element()
    {
        var report = Audit("<p id='a'>1</p><span id='a'>2</span><div id='a'>3</div>");
        var tags = report.Violations.Where(v => v.RuleId == "duplicate-id").Select(v => v.Html).ToArray();
        Assert.AreSequenceEqual(new[] { "<p id=\"a\">", "<span id=\"a\">", "<div id=\"a\">" }, tags);
    }

    // ── Report ───────────────────────────────────────────────────────────────

    [TestMethod]
    [DataRow("1st")]
    [DataRow("a'b\"c")]
    [DataRow("a b")]
    [DataRow("a:b.c")]
    [DataRow("-1")]
    [DataRow("#x")]
    public void Selector_escapes_ids(string id)
    {
        var body = Body("<p>x</p><p>y</p>");
        var p = body.QuerySelectorAll("p")[1];
        p.Id = id;
        Assert.AreSame(p, body.QuerySelector(BitBhA11y.Selector(p)));
    }

    [TestMethod]
    public void Json_report_keeps_its_shape()
    {
        var json = BitBhA11y.Audit(Body("<button id='b'></button>")).ToJson();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.AreSequenceEqual(new[] { "passed", "elements", "violations" }, root.EnumerateObject().Select(p => p.Name).ToArray());
        Assert.IsFalse(root.GetProperty("passed").GetBoolean());
        Assert.AreEqual(2, root.GetProperty("elements").GetInt32());
        var violation = root.GetProperty("violations")[0];
        Assert.AreSequenceEqual(new[] { "rule", "impact", "message", "selector", "html" }, violation.EnumerateObject().Select(p => p.Name).ToArray());
        Assert.AreEqual("accessible-name", violation.GetProperty("rule").GetString());
        Assert.AreEqual("critical", violation.GetProperty("impact").GetString());
        Assert.AreEqual("#b", violation.GetProperty("selector").GetString());
        Assert.AreEqual("<button id=\"b\">", violation.GetProperty("html").GetString());
        Assert.Contains("  \"passed\": false,", json);
    }
}
