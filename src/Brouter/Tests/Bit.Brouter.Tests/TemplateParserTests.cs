using Bit.Brouter;
using Xunit;

namespace Bit.Brouter.Tests;

public class TemplateParserTests
{
    [Fact]
    public void Empty_template_yields_empty_segments()
    {
        var result = TemplateParser.ParseTemplate("");
        Assert.Empty(result.TemplateSegments);
    }

    [Fact]
    public void Slash_template_is_handled()
    {
        var result = TemplateParser.ParseTemplate("/");
        Assert.Empty(result.TemplateSegments);
    }

    [Theory]
    [InlineData("/users")]
    [InlineData("users")]
    [InlineData("/users/")]
    public void Single_literal_parses_one_segment(string template)
    {
        var result = TemplateParser.ParseTemplate(template);
        Assert.Single(result.TemplateSegments);
    }

    [Fact]
    public void Optional_parameter_is_recognised()
    {
        var result = TemplateParser.ParseTemplate("/users/{id?}");
        Assert.True(result.TemplateSegments[1].IsOptional);
    }

    [Fact]
    public void Catch_all_parameter_is_recognised()
    {
        var result = TemplateParser.ParseTemplate("/files/{**path}");
        Assert.True(result.TemplateSegments[1].IsCatchAll);
        Assert.Equal("path", result.TemplateSegments[1].Value);
    }

    [Fact]
    public void Catch_all_must_be_last_segment()
    {
        Assert.Throws<InvalidOperationException>(() => TemplateParser.ParseTemplate("/files/{**path}/extra"));
    }

    [Fact]
    public void Optionals_must_be_trailing()
    {
        Assert.Throws<InvalidOperationException>(() => TemplateParser.ParseTemplate("/{a?}/{b}"));
    }

    [Fact]
    public void Duplicate_parameter_names_throw()
    {
        Assert.Throws<InvalidOperationException>(() => TemplateParser.ParseTemplate("/{id}/{id:int}"));
    }

    [Fact]
    public void Multiple_constraints_parse()
    {
        var result = TemplateParser.ParseTemplate("/{id:int:long}");
        Assert.Equal(2, result.TemplateSegments[0].Constraints.Length);
    }
}
