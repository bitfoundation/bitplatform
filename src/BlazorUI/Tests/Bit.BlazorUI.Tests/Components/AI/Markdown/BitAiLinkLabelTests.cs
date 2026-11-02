using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Markdown;

/// <summary>Finding 10: a link's text never contains another link.</summary>
[TestClass]
public class BitAiLinkLabelTests
{
    [TestMethod]
    [DataRow("[https://x.dev](https://x.dev)")]
    [DataRow("[<https://x.dev>](https://x.dev)")]
    [DataRow("[www.x.dev](https://x.dev)")]
    [DataRow("[see https://x.dev now](https://x.dev)")]
    public void AutoLinksInsideALinkLabelAreText(string source)
    {
        var link = Assert.IsExactInstanceOfType<BitAiLinkInline>(Assert.ContainsSingle(BitAiInlineParser.Parse(source)));
        Assert.IsEmpty(link.Inlines.OfType<BitAiLinkInline>());
    }
}
