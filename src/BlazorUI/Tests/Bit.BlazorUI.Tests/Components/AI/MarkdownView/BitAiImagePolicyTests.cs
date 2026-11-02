using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.MarkdownView;

[TestClass]
public class BitAiImagePolicyTests
{
    [TestMethod]
    public void ThePolicyDecidesPerUrl()
    {
        var listed = BitAiImagePolicy.AllowListedHosts("cdn.x.dev");

        Assert.AreEqual(BitAiImageDecision.Show, listed.Decide("https://cdn.x.dev/a.png"));
        Assert.AreEqual(BitAiImageDecision.Show, listed.Decide("https://CDN.X.DEV/a.png"));
        Assert.AreEqual(BitAiImageDecision.AskFirst, listed.Decide("https://other.dev/a.png"));
        Assert.AreEqual(BitAiImageDecision.AskFirst, listed.Decide("not a url"));
        Assert.AreEqual(BitAiImageDecision.Show, listed.Decide("data:image/png;base64,AAA"));
        Assert.AreEqual(BitAiImageDecision.Hide, BitAiImagePolicy.Block.Decide("data:image/png;base64,AAA"));
        Assert.AreEqual(BitAiImageDecision.Hide, (BitAiImagePolicy.Allow with { AllowDataImages = false }).Decide("data:image/png;base64,AAA"));
    }

    [TestMethod]
    public void AWildcardHostMatchesSubdomainsOnly()
    {
        var listed = BitAiImagePolicy.AllowListedHosts("*.trusted.org", " ", "");

        Assert.AreEqual(BitAiImageDecision.Show, listed.Decide("https://img.trusted.org/a.png"));
        Assert.AreEqual(BitAiImageDecision.AskFirst, listed.Decide("https://trusted.org/a.png"));
        Assert.AreEqual(BitAiImageDecision.AskFirst, listed.Decide("https://untrusted.org/a.png"));
        Assert.HasCount(1, listed.Hosts);
    }

    [TestMethod]
    public void TheBuiltInPoliciesSayWhatTheyAreNamedFor()
    {
        Assert.AreEqual(BitAiImageDecision.Show, BitAiImagePolicy.Allow.Decide("https://x.dev/a.png"));
        Assert.AreEqual(BitAiImageDecision.Hide, BitAiImagePolicy.Block.Decide("https://x.dev/a.png"));
        Assert.AreEqual(BitAiImageDecision.AskFirst, BitAiImagePolicy.ClickToLoad.Decide("https://x.dev/a.png"));
        Assert.AreEqual(BitAiImageDecision.Show, BitAiImagePolicy.ClickToLoad.Decide("data:image/png;base64,AAA"));
    }
}
