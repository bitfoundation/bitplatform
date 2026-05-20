using System.Reflection;
using Bit.Brouter;
using Xunit;

namespace Bit.Brouter.Tests;

public class TemplateParserTests
{
    private static object Parse(string template)
    {
        var parserType = typeof(BrouterConstraints).Assembly.GetType("Bit.Brouter.TemplateParser")!;
        return parserType.GetMethod("ParseTemplate", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [template])!;
    }

    [Fact]
    public void Empty_template_yields_empty_segments()
    {
        var result = Parse("");
        var segs = (Array)result.GetType().GetProperty("TemplateSegments")!.GetValue(result)!;
        Assert.Empty(segs);
    }

    [Fact]
    public void Slash_template_is_handled()
    {
        var result = Parse("/");
        var segs = (Array)result.GetType().GetProperty("TemplateSegments")!.GetValue(result)!;
        Assert.Empty(segs);
    }

    [Theory]
    [InlineData("/users")]
    [InlineData("users")]
    [InlineData("/users/")]
    public void Single_literal_parses_one_segment(string template)
    {
        var result = Parse(template);
        var segs = (Array)result.GetType().GetProperty("TemplateSegments")!.GetValue(result)!;
        Assert.Single(segs);
    }

    [Fact]
    public void Optional_parameter_is_recognised()
    {
        var result = Parse("/users/{id?}");
        var segs = (Array)result.GetType().GetProperty("TemplateSegments")!.GetValue(result)!;
        var optional = segs.GetValue(1)!;
        Assert.True((bool)optional.GetType().GetProperty("IsOptional")!.GetValue(optional)!);
    }

    [Fact]
    public void Catch_all_parameter_is_recognised()
    {
        var result = Parse("/files/{**path}");
        var segs = (Array)result.GetType().GetProperty("TemplateSegments")!.GetValue(result)!;
        var catchAll = segs.GetValue(1)!;
        Assert.True((bool)catchAll.GetType().GetProperty("IsCatchAll")!.GetValue(catchAll)!);
        Assert.Equal("path", catchAll.GetType().GetProperty("Value")!.GetValue(catchAll));
    }

    [Fact]
    public void Catch_all_must_be_last_segment()
    {
        var ex = Assert.Throws<TargetInvocationException>(() => Parse("/files/{**path}/extra"));
        Assert.IsType<InvalidOperationException>(ex.InnerException);
    }

    [Fact]
    public void Optionals_must_be_trailing()
    {
        var ex = Assert.Throws<TargetInvocationException>(() => Parse("/{a?}/{b}"));
        Assert.IsType<InvalidOperationException>(ex.InnerException);
    }

    [Fact]
    public void Duplicate_parameter_names_throw()
    {
        var ex = Assert.Throws<TargetInvocationException>(() => Parse("/{id}/{id:int}"));
        Assert.IsType<InvalidOperationException>(ex.InnerException);
    }

    [Fact]
    public void Multiple_constraints_parse()
    {
        var result = Parse("/{id:int:long}");
        var segs = (Array)result.GetType().GetProperty("TemplateSegments")!.GetValue(result)!;
        var seg = segs.GetValue(0)!;
        var constraints = (Array)seg.GetType().GetProperty("Constraints")!.GetValue(seg)!;
        Assert.Equal(2, constraints.Length);
    }
}
