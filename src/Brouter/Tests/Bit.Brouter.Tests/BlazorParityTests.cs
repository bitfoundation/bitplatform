using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

/// <summary>
/// End-to-end matching tests for the template features shared with the built-in Blazor router:
/// middle optional parameters, single-star / constrained catch-alls, default values, complex
/// multi-part segments, and the parameterized built-in constraints.
/// </summary>
[TestClass]
public class BlazorParityTests : BunitTestContext
{
    private IRenderedComponent<BlazorParityHost> RenderAt(string url)
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost" + url);
        return RenderComponent<BlazorParityHost>();
    }

    [TestMethod]
    public void Middle_optional_matches_when_the_segment_is_supplied()
    {
        var cut = RenderAt("/products/en/list");
        cut.WaitForAssertion(() => Assert.AreEqual("en", cut.Find("[data-testid=products]").TextContent));
    }

    [TestMethod]
    public void Middle_optional_behaves_as_required_when_omitted()
    {
        // Framework parity: "/products/{culture?}/list" never matches "/products/list" - the
        // built-in router ignores optional-ness for non-trailing parameters.
        var cut = RenderAt("/products/list");
        cut.WaitForAssertion(() => Assert.AreEqual(0, cut.FindAll("[data-testid=products]").Count));
    }

    [TestMethod]
    public void Constrained_middle_optional_matches_a_non_file_first_segment()
    {
        var cut = RenderAt("/en/home");
        cut.WaitForAssertion(() => Assert.AreEqual("en", cut.Find("[data-testid=home]").TextContent));
    }

    [TestMethod]
    public void Constrained_middle_optional_rejects_a_file_name_segment()
    {
        var cut = RenderAt("/style.css/home");
        cut.WaitForAssertion(() => Assert.AreEqual(0, cut.FindAll("[data-testid=home]").Count));
    }

    [TestMethod]
    public void Complex_segment_binds_both_parts()
    {
        var cut = RenderAt("/files/report.pdf");
        cut.WaitForAssertion(() => Assert.AreEqual("report|pdf", cut.Find("[data-testid=file]").TextContent));
    }

    [TestMethod]
    public void Complex_segment_optional_part_may_be_omitted()
    {
        var cut = RenderAt("/files/report");
        cut.WaitForAssertion(() => Assert.AreEqual("report|(none)", cut.Find("[data-testid=file]").TextContent));
    }

    [TestMethod]
    public void Complex_segment_rejects_a_trailing_separator()
    {
        // "report." must not match "{name}.{ext?}" (framework parity).
        var cut = RenderAt("/files/report.");
        cut.WaitForAssertion(() => Assert.AreEqual(0, cut.FindAll("[data-testid=file]").Count));
    }

    [TestMethod]
    public void Complex_segment_optional_part_binds_the_rightmost_extension()
    {
        // "a.b.c" binds name="a.b", ext="c" - the rightmost separator wins (framework parity).
        var cut = RenderAt("/files/a.b.c");
        cut.WaitForAssertion(() => Assert.AreEqual("a.b|c", cut.Find("[data-testid=file]").TextContent));
    }

    [TestMethod]
    public void Complex_segment_with_multiple_typed_parameters_matches()
    {
        var cut = RenderAt("/api/v2-10");
        cut.WaitForAssertion(() => Assert.AreEqual("2.10", cut.Find("[data-testid=version]").TextContent));
    }

    [TestMethod]
    public void Complex_segment_typed_parameter_rejects_a_non_matching_value()
    {
        var cut = RenderAt("/api/vX-10");
        cut.WaitForAssertion(() => Assert.AreEqual(0, cut.FindAll("[data-testid=version]").Count));
    }

    [TestMethod]
    public void Constrained_catch_all_matches_a_directory_remainder()
    {
        var cut = RenderAt("/static/docs/guide");
        cut.WaitForAssertion(() => Assert.AreEqual("docs/guide", cut.Find("[data-testid=static]").TextContent));
    }

    [TestMethod]
    public void Nonfile_catch_all_accepts_an_empty_remainder()
    {
        var cut = RenderAt("/static");
        cut.WaitForAssertion(() => Assert.AreEqual("(empty)", cut.Find("[data-testid=static]").TextContent));
    }

    [TestMethod]
    public void Nonfile_catch_all_rejects_a_file_name_remainder()
    {
        var cut = RenderAt("/static/app.js");
        cut.WaitForAssertion(() => Assert.AreEqual(0, cut.FindAll("[data-testid=static]").Count));
    }

    [TestMethod]
    public void Default_value_is_bound_when_the_segment_is_omitted()
    {
        var cut = RenderAt("/blog");
        cut.WaitForAssertion(() => Assert.AreEqual("Index", cut.Find("[data-testid=blog]").TextContent));
    }

    [TestMethod]
    public void Default_value_is_overridden_by_the_url()
    {
        var cut = RenderAt("/blog/Archive");
        cut.WaitForAssertion(() => Assert.AreEqual("Archive", cut.Find("[data-testid=blog]").TextContent));
    }

    [TestMethod]
    public void Validation_constraints_compose_with_typed_conversion()
    {
        // min/max validate but the int constraint's conversion is what binds.
        var cut = RenderAt("/rate/3");
        cut.WaitForAssertion(() => Assert.AreEqual("Int32:3", cut.Find("[data-testid=rate]").TextContent));
    }

    [TestMethod]
    [DataRow("/rate/0")]
    [DataRow("/rate/6")]
    [DataRow("/rate/abc")]
    public void Validation_constraints_reject_out_of_range_values(string url)
    {
        var cut = RenderAt(url);
        cut.WaitForAssertion(() => Assert.AreEqual(0, cut.FindAll("[data-testid=rate]").Count));
    }
}
