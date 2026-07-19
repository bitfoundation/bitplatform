using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

[TestClass]
public class TemplateParserTests
{
    [TestMethod]
    public void Empty_template_yields_empty_segments()
    {
        var result = BrouterTemplateParser.ParseTemplate("");
        Assert.AreEqual(0, result.TemplateSegments.Count);
    }

    [TestMethod]
    public void Slash_template_is_handled()
    {
        var result = BrouterTemplateParser.ParseTemplate("/");
        Assert.AreEqual(0, result.TemplateSegments.Count);
    }

    [TestMethod]
    [DataRow("/users")]
    [DataRow("users")]
    [DataRow("/users/")]
    [DataRow("~/users")]
    public void Single_literal_parses_one_segment(string template)
    {
        var result = BrouterTemplateParser.ParseTemplate(template);
        Assert.AreEqual(1, result.TemplateSegments.Count);
    }

    [TestMethod]
    public void Optional_parameter_is_recognised()
    {
        var result = BrouterTemplateParser.ParseTemplate("/users/{id?}");
        Assert.IsTrue(result.TemplateSegments[1].IsOptional);
    }

    [TestMethod]
    public void Catch_all_parameter_is_recognised()
    {
        var result = BrouterTemplateParser.ParseTemplate("/files/{**path}");
        Assert.IsTrue(result.TemplateSegments[1].IsCatchAll);
        Assert.AreEqual("path", result.TemplateSegments[1].Value);
    }

    [TestMethod]
    public void Single_star_catch_all_parameter_is_recognised()
    {
        var result = BrouterTemplateParser.ParseTemplate("/files/{*path}");
        Assert.IsTrue(result.TemplateSegments[1].IsCatchAll);
        Assert.AreEqual("path", result.TemplateSegments[1].Value);
    }

    [TestMethod]
    public void Catch_all_must_be_last_segment()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => BrouterTemplateParser.ParseTemplate("/files/{**path}/extra"));
    }

    [TestMethod]
    [DataRow("/files/{*path?}")]
    [DataRow("/files/{**path?}")]
    public void Optional_catch_all_throws(string template)
    {
        // Framework parity: a catch-all already matches zero segments, so '?' is rejected.
        Assert.ThrowsExactly<ArgumentException>(() => BrouterTemplateParser.ParseTemplate(template));
    }

    [TestMethod]
    public void Catch_all_may_declare_constraints()
    {
        var result = BrouterTemplateParser.ParseTemplate("/files/{*path:nonfile}");
        Assert.IsTrue(result.TemplateSegments[1].IsCatchAll);
        Assert.AreEqual(1, result.TemplateSegments[1].Constraints.Length);
        Assert.AreEqual("nonfile", result.TemplateSegments[1].Constraints[0].Name);
    }

    [TestMethod]
    public void Middle_optional_parameter_parses()
    {
        // Framework parity: "/products/{culture?}/list" is a valid template. The optional-ness is
        // ignored at match time (the parameter behaves as required), but parsing must accept it.
        var result = BrouterTemplateParser.ParseTemplate("/products/{culture?}/list");
        Assert.AreEqual(3, result.TemplateSegments.Count);
        Assert.IsTrue(result.TemplateSegments[1].IsOptional);
        Assert.AreEqual("culture", result.TemplateSegments[1].Value);
    }

    [TestMethod]
    public void Middle_optional_with_constraint_parses()
    {
        var result = BrouterTemplateParser.ParseTemplate("/{lang:nonfile?}/home");
        Assert.IsTrue(result.TemplateSegments[0].IsOptional);
        Assert.AreEqual("nonfile", result.TemplateSegments[0].Constraints[0].Name);
    }

    [TestMethod]
    public void Default_value_parses()
    {
        var result = BrouterTemplateParser.ParseTemplate("/blog/{action=Index}");
        Assert.IsTrue(result.TemplateSegments[1].HasDefault);
        Assert.AreEqual("Index", result.TemplateSegments[1].DefaultValue);
        Assert.AreEqual("action", result.TemplateSegments[1].Value);
    }

    [TestMethod]
    public void Middle_default_value_parses()
    {
        // Framework parity: like a middle optional, a non-trailing default parses fine and simply
        // behaves as required at match time (defaults only bind when a trailing run is omitted).
        var result = BrouterTemplateParser.ParseTemplate("/{a=x}/{b}");
        Assert.AreEqual(2, result.TemplateSegments.Count);
        Assert.IsTrue(result.TemplateSegments[0].HasDefault);
        Assert.AreEqual("x", result.TemplateSegments[0].DefaultValue);
    }

    [TestMethod]
    public void Default_value_with_constraint_parses()
    {
        var segment = BrouterTemplateParser.ParseTemplate("/blog/{id:int=5}").TemplateSegments[1];
        Assert.AreEqual("5", segment.DefaultValue);
        Assert.AreEqual(1, segment.Constraints.Length);
        Assert.AreEqual("int", segment.Constraints[0].Name);
    }

    [TestMethod]
    public void Optional_with_default_value_throws()
    {
        Assert.ThrowsExactly<ArgumentException>(() => BrouterTemplateParser.ParseTemplate("/blog/{id=5?}"));
    }

    [TestMethod]
    public void Duplicate_parameter_names_throw()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => BrouterTemplateParser.ParseTemplate("/{id}/{id:int}"));
    }

    [TestMethod]
    public void Duplicate_parameter_names_across_complex_segments_throw()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => BrouterTemplateParser.ParseTemplate("/{id}/x-{id}"));
    }

    [TestMethod]
    public void Multiple_constraints_parse()
    {
        var result = BrouterTemplateParser.ParseTemplate("/{id:int:long}");
        Assert.AreEqual(2, result.TemplateSegments[0].Constraints.Length);
    }

    [TestMethod]
    public void Parameterized_constraints_split_parenthesis_aware()
    {
        var result = BrouterTemplateParser.ParseTemplate("/{v:range(1,5)}");
        Assert.AreEqual(1, result.TemplateSegments[0].Constraints.Length);
        Assert.AreEqual("range(1,5)", result.TemplateSegments[0].Constraints[0].Name);

        result = BrouterTemplateParser.ParseTemplate("/{v:min(1):max(5)}");
        Assert.AreEqual(2, result.TemplateSegments[0].Constraints.Length);
    }

    [TestMethod]
    public void Complex_segment_parses_into_parts()
    {
        var result = BrouterTemplateParser.ParseTemplate("/files/{name}.{ext}");
        var segment = result.TemplateSegments[1];

        Assert.IsTrue(segment.IsComplex);
        Assert.AreEqual(3, segment.Parts!.Count);
        Assert.AreEqual("name", segment.Parts[0].Value);
        Assert.IsFalse(segment.Parts[1].IsParameter);
        Assert.AreEqual("ext", segment.Parts[2].Value);
        CollectionAssert.AreEquivalent(new[] { "name", "ext" }, segment.ParameterNames.ToArray());
    }

    [TestMethod]
    public void Complex_segment_optional_last_part_converts_period_to_separator()
    {
        var result = BrouterTemplateParser.ParseTemplate("/files/{name}.{ext?}");
        var segment = result.TemplateSegments[1];

        Assert.IsTrue(segment.Parts![1].IsSeparator);
        Assert.IsTrue(segment.Parts[2].IsOptional);
    }

    [TestMethod]
    public void Complex_segment_optional_must_be_preceded_by_period()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => BrouterTemplateParser.ParseTemplate("/files/{name}-{ext?}"));
    }

    [TestMethod]
    public void Complex_segment_rejects_consecutive_parameters()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => BrouterTemplateParser.ParseTemplate("/files/{a}{b}"));
    }

    [TestMethod]
    public void Complex_segment_rejects_catch_all_parts()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => BrouterTemplateParser.ParseTemplate("/files/x{*rest}"));
    }

    [TestMethod]
    public void Escaped_braces_produce_a_literal_segment()
    {
        var result = BrouterTemplateParser.ParseTemplate("/docs/{{literal}}");
        var segment = result.TemplateSegments[1];

        Assert.IsFalse(segment.IsParameter);
        Assert.IsFalse(segment.IsComplex);
        Assert.AreEqual("{literal}", segment.Value);
    }
}
