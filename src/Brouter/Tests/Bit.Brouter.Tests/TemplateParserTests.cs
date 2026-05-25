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
    public void Catch_all_must_be_last_segment()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => BrouterTemplateParser.ParseTemplate("/files/{**path}/extra"));
    }

    [TestMethod]
    public void Optionals_must_be_trailing()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => BrouterTemplateParser.ParseTemplate("/{a?}/{b}"));
    }

    [TestMethod]
    public void Duplicate_parameter_names_throw()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => BrouterTemplateParser.ParseTemplate("/{id}/{id:int}"));
    }

    [TestMethod]
    public void Multiple_constraints_parse()
    {
        var result = BrouterTemplateParser.ParseTemplate("/{id:int:long}");
        Assert.AreEqual(2, result.TemplateSegments[0].Constraints.Length);
    }
}
