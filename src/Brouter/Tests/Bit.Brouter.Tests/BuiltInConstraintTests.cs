using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

/// <summary>
/// Unit tests for the framework-parity built-in constraints (alpha, file, nonfile, min, max,
/// range, minlength, maxlength, length, regex) and their interaction with typed conversions.
/// </summary>
[TestClass]
public class BuiltInConstraintTests
{
    private static BrouterTemplateSegment Segment(string template) =>
        BrouterTemplateParser.ParseTemplate(template).TemplateSegments[0];

    private static bool Match(string template, string value, out object? converted) =>
        Segment(template).TryMatch(value, StringComparison.OrdinalIgnoreCase, out converted);

    [TestMethod]
    [DataRow("abc", true)]
    [DataRow("ABC", true)]
    [DataRow("", true)] // ^[A-Za-z]*$ accepts empty, framework parity
    [DataRow("ab1", false)]
    [DataRow("a-b", false)]
    public void Alpha_matches_ascii_letters_only(string value, bool expected) =>
        Assert.AreEqual(expected, Match("/{v:alpha}", value, out _));

    [TestMethod]
    [DataRow("a.txt", true)]
    [DataRow(".gitignore", true)]
    [DataRow("a", false)]
    [DataRow("a.", false)]
    [DataRow("a...", false)]
    public void File_matches_file_names(string value, bool expected) =>
        Assert.AreEqual(expected, Match("/{v:file}", value, out _));

    [TestMethod]
    [DataRow("docs", true)]
    [DataRow("app.js", false)]
    public void Nonfile_is_the_negation(string value, bool expected) =>
        Assert.AreEqual(expected, Match("/{v:nonfile}", value, out _));

    [TestMethod]
    public void Min_max_range_validate_the_numeric_value()
    {
        Assert.IsTrue(Match("/{v:min(3)}", "3", out _));
        Assert.IsFalse(Match("/{v:min(3)}", "2", out _));
        Assert.IsFalse(Match("/{v:min(3)}", "abc", out _));

        Assert.IsTrue(Match("/{v:max(3)}", "3", out _));
        Assert.IsFalse(Match("/{v:max(3)}", "4", out _));

        Assert.IsTrue(Match("/{v:range(2,4)}", "3", out _));
        Assert.IsFalse(Match("/{v:range(2,4)}", "5", out _));
        Assert.IsFalse(Match("/{v:range(2,4)}", "1", out _));
    }

    [TestMethod]
    public void Length_constraints_validate_the_text_length()
    {
        Assert.IsTrue(Match("/{v:minlength(2)}", "ab", out _));
        Assert.IsFalse(Match("/{v:minlength(2)}", "a", out _));

        Assert.IsTrue(Match("/{v:maxlength(2)}", "ab", out _));
        Assert.IsFalse(Match("/{v:maxlength(2)}", "abc", out _));

        Assert.IsTrue(Match("/{v:length(2)}", "ab", out _));
        Assert.IsFalse(Match("/{v:length(2)}", "abc", out _));

        Assert.IsTrue(Match("/{v:length(2,3)}", "abc", out _));
        Assert.IsFalse(Match("/{v:length(2,3)}", "abcd", out _));
    }

    [TestMethod]
    public void Regex_constraint_validates_with_the_inline_pattern()
    {
        Assert.IsTrue(Match(@"/{v:regex(^\d+$)}", "123", out _));
        Assert.IsFalse(Match(@"/{v:regex(^\d+$)}", "12a", out _));
    }

    [TestMethod]
    public void Validation_constraints_keep_the_value_a_string()
    {
        Assert.IsTrue(Match("/{v:min(1)}", "5", out var converted));
        Assert.AreEqual("5", converted);
    }

    [TestMethod]
    public void Validation_constraints_do_not_clobber_a_typed_conversion()
    {
        // {id:int:min(0)} must still bind an int even though min runs last.
        Assert.IsTrue(Match("/{id:int:min(0)}", "5", out var converted));
        Assert.AreEqual(5, converted);
    }

    [TestMethod]
    public void Type_constraint_after_a_validation_constraint_still_converts()
    {
        Assert.IsTrue(Match("/{id:min(0):int}", "5", out var converted));
        Assert.AreEqual(5, converted);
    }

    [TestMethod]
    public void Parameterized_names_cannot_be_registered_as_custom_constraints()
    {
        var registry = new BrouterConstraintRegistry();
        Assert.ThrowsExactly<InvalidOperationException>(() => registry.Register("min", new BrouterPredicateRouteConstraint(_ => true)));
        Assert.ThrowsExactly<InvalidOperationException>(() => registry.Register("nonfile", new BrouterPredicateRouteConstraint(_ => true)));
    }

    [TestMethod]
    public void Malformed_constraint_argument_throws()
    {
        Assert.ThrowsExactly<ArgumentException>(() => BrouterTemplateParser.ParseTemplate("/{v:min(abc)}"));
        Assert.ThrowsExactly<ArgumentException>(() => BrouterTemplateParser.ParseTemplate("/{v:range(1)}"));
    }
}
