using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Markdown;

/// <summary>Finding 4: dollar signs follow pandoc's rules, so prices are not mathematics.</summary>
[TestClass]
public class BitAiInlineMathTests
{
    [TestMethod]
    [DataRow("costs $5 and $10 total")]
    [DataRow("It is $5.")]
    [DataRow("a $10,000 budget and $20,000 more")]
    [DataRow("$ a$")]
    [DataRow("$a $")]
    [DataRow("$5 or $6")]
    public void PricesAreNotMath(string source) =>
        Assert.IsEmpty(BitAiInlineParser.Parse(source).OfType<BitAiMathInline>());

    [TestMethod]
    [DataRow("$x^2$", "x^2")]
    [DataRow("between $5 and $x$ here", "x")]
    [DataRow("$x^2$ costs $5", "x^2")]
    [DataRow("$2x + 1 = 5$", "2x + 1 = 5")]
    [DataRow("$\\alpha$", "\\alpha")]
    public void ExpressionsHuggedByTheirDelimitersAreMath(string source, string expression) =>
        Assert.AreEqual(expression, BitAiInlineParser.Parse(source).OfType<BitAiMathInline>().Single().Expression);
}
