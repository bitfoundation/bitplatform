using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

[TestClass]
public class QueryBindingTests : BunitTestContext
{
    [TestMethod]
    public void Scalar_string_query_binds_to_string_property()
    {
        var (cut, _) = RenderAt<QueryHost>("http://localhost/q?name=saleh");

        cut.WaitForAssertion(() => Assert.AreEqual("saleh", cut.Find("[data-testid=name]").TextContent));
    }

    [TestMethod]
    public void Numeric_query_binds_to_int_and_nullable_int()
    {
        var (cut, _) = RenderAt<QueryHost>("http://localhost/q?count=7&nullableCount=42");

        cut.WaitForAssertion(() => Assert.AreEqual("7", cut.Find("[data-testid=count]").TextContent));
        cut.WaitForAssertion(() => Assert.AreEqual("42", cut.Find("[data-testid=ncount]").TextContent));
    }

    [TestMethod]
    public void Missing_query_leaves_property_at_default()
    {
        var (cut, _) = RenderAt<QueryHost>("http://localhost/q");

        // Reference types stay null, value types stay default(T).
        cut.WaitForAssertion(() => Assert.AreEqual("(null)", cut.Find("[data-testid=name]").TextContent));
        cut.WaitForAssertion(() => Assert.AreEqual("0", cut.Find("[data-testid=count]").TextContent));
        cut.WaitForAssertion(() => Assert.AreEqual("(null)", cut.Find("[data-testid=ncount]").TextContent));
        cut.WaitForAssertion(() => Assert.AreEqual("(null)", cut.Find("[data-testid=tags]").TextContent));
    }

    [TestMethod]
    public void Guid_query_value_converts_correctly()
    {
        var id = Guid.NewGuid();
        var (cut, _) = RenderAt<QueryHost>($"http://localhost/q?id={id}");

        cut.WaitForAssertion(() => Assert.AreEqual(id.ToString(), cut.Find("[data-testid=id]").TextContent));
    }

    [TestMethod]
    public void Enum_query_value_binds_via_BrouterQuery()
    {
        // Enums are outside the framework query supplier's supported types (it throws for them when
        // the property uses [SupplyParameterFromQuery]); [BrouterQuery] is the opt-in escape hatch -
        // the supplier ignores it, and Brouter's own converter handles the enum. This render (with
        // bUnit's framework supplier active) also proves the supplier leaves the property alone.
        var (cut, _) = RenderAt<QueryHost>("http://localhost/q?day=Tuesday");

        cut.WaitForAssertion(() => Assert.AreEqual("Tuesday", cut.Find("[data-testid=day]").TextContent));
    }

    [TestMethod]
    public void Enum_query_value_is_parsed_case_insensitively()
    {
        // TryConvert calls Enum.TryParse with ignoreCase: true.
        var (cut, _) = RenderAt<QueryHost>("http://localhost/q?day=tuesday");

        cut.WaitForAssertion(() => Assert.AreEqual("Tuesday", cut.Find("[data-testid=day]").TextContent));
    }

    [TestMethod]
    public void Aliased_query_uses_attribute_name_over_property_name()
    {
        // The "Aliased" property is annotated with [SupplyParameterFromQuery(Name = "q")];
        // its property name should NOT be matched.
        var (cut, _) = RenderAt<QueryHost>("http://localhost/q?q=hello&aliased=ignored");

        cut.WaitForAssertion(() => Assert.AreEqual("hello", cut.Find("[data-testid=aliased]").TextContent));
    }

    [TestMethod]
    public void Multi_value_query_binds_string_array_in_order()
    {
        var (cut, _) = RenderAt<QueryHost>("http://localhost/q?tags=a&tags=b&tags=c");

        cut.WaitForAssertion(() => Assert.AreEqual("a,b,c", cut.Find("[data-testid=tags]").TextContent));
    }

    [TestMethod]
    public void Query_keys_are_matched_case_insensitively()
    {
        // Library-wide convention (route params, ResolveUrl dictionaries) is OrdinalIgnoreCase.
        // BrouterLocation.QueryParams now follows the same rule, so "Name" should bind to the
        // property declared as "name".
        var (cut, _) = RenderAt<QueryHost>("http://localhost/q?Name=Saleh&COUNT=9");

        cut.WaitForAssertion(() => Assert.AreEqual("Saleh", cut.Find("[data-testid=name]").TextContent));
        cut.WaitForAssertion(() => Assert.AreEqual("9", cut.Find("[data-testid=count]").TextContent));
    }

    [TestMethod]
    public void Percent_encoded_and_plus_separated_query_values_are_decoded()
    {
        // '+' decodes to ' ', and percent-encoded chars are unescaped.
        var (cut, _) = RenderAt<QueryHost>("http://localhost/q?name=hello+world&q=a%26b");

        cut.WaitForAssertion(() => Assert.AreEqual("hello world", cut.Find("[data-testid=name]").TextContent));
        cut.WaitForAssertion(() => Assert.AreEqual("a&b", cut.Find("[data-testid=aliased]").TextContent));
    }

    [TestMethod]
    public void Query_re_binds_when_navigation_changes_the_query_string()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/q?name=first");

        var cut = RenderComponent<QueryHost>();
        cut.WaitForAssertion(() => Assert.AreEqual("first", cut.Find("[data-testid=name]").TextContent));

        nav.NavigateTo("http://localhost/q?name=second");
        cut.WaitForAssertion(() => Assert.AreEqual("second", cut.Find("[data-testid=name]").TextContent));
    }

    // ---- BrouterLocation.QueryParams direct tests ----------------------------------------

    [TestMethod]
    public void Location_query_parses_multi_value_keys_into_a_single_entry()
    {
        var loc = new BrouterLocation(
            "http://localhost/q?tags=a&tags=b",
            "/q", ["q"], "?tags=a&tags=b", "");

        Assert.IsTrue(loc.QueryParams.ContainsKey("tags"));
        var values = loc.QueryParams["tags"];
        Assert.AreEqual(2, values.Count);
        Assert.AreEqual("a", values[0]);
        Assert.AreEqual("b", values[1]);
    }

    [TestMethod]
    public void Location_GetQuery_returns_first_value_and_GetQueryAll_returns_all()
    {
        var loc = new BrouterLocation(
            "http://localhost/q?tag=x&tag=y",
            "/q", ["q"], "?tag=x&tag=y", "");

        Assert.AreEqual("x", loc.GetQuery("tag"));
        var all = loc.GetQueryAll("tag");
        Assert.AreEqual(2, all.Count);
        CollectionAssert.AreEqual(new[] { "x", "y" }, all.ToArray());
    }

    [TestMethod]
    public void Location_query_lookup_is_case_insensitive()
    {
        var loc = new BrouterLocation(
            "http://localhost/q?Tab=1",
            "/q", ["q"], "?Tab=1", "");

        // Both casings should resolve, mirroring ASP.NET Core's IQueryCollection.
        Assert.AreEqual("1", loc.GetQuery("Tab"));
        Assert.AreEqual("1", loc.GetQuery("tab"));
        Assert.AreEqual("1", loc.GetQuery("TAB"));
    }

    [TestMethod]
    public void Location_query_handles_keys_without_values_and_empty_pairs()
    {
        // "flag" has no '=' (no value); "&&" is two empty pairs that should be skipped;
        // "x=" has an empty value.
        var loc = new BrouterLocation(
            "http://localhost/q?flag&&x=&y=2",
            "/q", ["q"], "?flag&&x=&y=2", "");

        Assert.AreEqual(string.Empty, loc.GetQuery("flag"));
        Assert.AreEqual(string.Empty, loc.GetQuery("x"));
        Assert.AreEqual("2", loc.GetQuery("y"));
    }

    [TestMethod]
    public void Location_GetQuery_returns_null_when_key_is_missing()
    {
        var loc = new BrouterLocation(
            "http://localhost/q?a=1",
            "/q", ["q"], "?a=1", "");

        Assert.IsNull(loc.GetQuery("missing"));
        Assert.AreEqual(0, loc.GetQueryAll("missing").Count);
    }

    [TestMethod]
    public void Location_query_does_not_throw_on_malformed_percent_encoding()
    {
        // Decode falls back to the raw (with '+' -> ' ') string when Uri.UnescapeDataString
        // throws UriFormatException, so query parsing must keep working.
        var loc = new BrouterLocation(
            "http://localhost/q?name=%ZZ+ok",
            "/q", ["q"], "?name=%ZZ+ok", "");

        string? value = null;
        IReadOnlyList<string>? all = null;

        // Both calls must complete without throwing.
        value = loc.GetQuery("name");
        all = loc.GetQueryAll("name");

        Assert.IsNotNull(value);
        StringAssert.Contains(value, "ok");
        Assert.IsNotNull(all);
        Assert.AreEqual(1, all.Count);
        StringAssert.Contains(all[0], "ok");
    }
}
