using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Markdown;

/// <summary>Finding 2: unmatched emphasis openers cost linear time.</summary>
[TestClass, DoNotParallelize]
public class BitAiUnmatchedEmphasisPerformanceTests
{
    [TestMethod]
    [DataRow("*a ")]
    [DataRow("_a ")]
    [DataRow("**a ")]
    public void OneHundredEightyKilobytesOfUnmatchedOpenersParseInWellUnderASecond(string pattern)
    {
        var source = BitAiMarkdownSpec.Repeat(pattern, 180_000 / pattern.Length);
        var elapsed = BitAiMarkdownSpec.Time(() => BitAiMarkdownParser.Parse(source));

        // Measured at about 50ms in Release; the limit leaves room for a busy CI machine while
        // staying an order of magnitude under the four seconds the quadratic scan took.
        Assert.IsTrue(elapsed < TimeSpan.FromMilliseconds(400), $"'{pattern}' took {elapsed.TotalMilliseconds:F0}ms for {source.Length} characters.");
    }
}
