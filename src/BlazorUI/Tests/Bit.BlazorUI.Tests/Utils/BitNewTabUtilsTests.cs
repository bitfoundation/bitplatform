using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils;

[TestClass]
public sealed class BitNewTabUtilsTests
{
    [TestMethod,
        DataRow("_blank", true),
        DataRow("_BLANK", true),
        DataRow("_Blank", true),
        DataRow("_self", false),
        DataRow("blank", false),
        DataRow(" _blank", false),
        DataRow("", false),
        DataRow(null, false)
    ]
    public void IsNewTabShouldMatchTheBlankKeywordCaseInsensitively(string? target, bool expected)
    {
        // The browser opens a new tab for "_BLANK" just as it does for "_blank", so both have to be
        // hardened and announced - but a target that is not the keyword itself is a named frame.
        Assert.AreEqual(expected, BitNewTabUtils.IsNewTab(target));
    }

    [TestMethod,
        DataRow(null, "_blank", "noopener"),
        DataRow("", "_blank", "noopener"),
        DataRow("   ", "_blank", "noopener"),
        DataRow(null, "_BLANK", "noopener"),
        DataRow("nofollow", "_blank", "nofollow noopener"),
        DataRow("nofollow", "_Blank", "nofollow noopener"),
        DataRow("noopener-policy", "_blank", "noopener-policy noopener"),
        DataRow("nofollow", "_self", "nofollow"),
        DataRow(null, "_self", null),
        DataRow(null, null, null)
    ]
    public void AddNoOpenerShouldAddNoOpenerToANewTabRel(string? rel, string? target, string? expected)
    {
        Assert.AreEqual(expected, BitNewTabUtils.AddNoOpener(rel, target));
    }

    [TestMethod,
        DataRow("noopener"),
        DataRow("NoOpener"),
        DataRow("noreferrer"),
        DataRow("nofollow noreferrer"),
        DataRow("opener"),
        DataRow("external opener")
    ]
    public void AddNoOpenerShouldLeaveARelThatAlreadySaysWhatTheOpenerShouldBe(string rel)
    {
        // noreferrer already implies noopener, and an author asking for opener back means it.
        Assert.AreEqual(rel, BitNewTabUtils.AddNoOpener(rel, "_blank"));
    }

    [TestMethod,
        DataRow("_blank", null, false, BitNewTabUtils.DefaultHint),
        DataRow("_BLANK", null, false, BitNewTabUtils.DefaultHint),
        DataRow("_blank", "(new window)", false, "(new window)"),
        DataRow("_blank", "", false, null),
        DataRow("_blank", "   ", false, null),
        DataRow("_blank", null, true, null),
        DataRow("_blank", "(new window)", true, null),
        DataRow("_self", null, false, null),
        DataRow(null, "(new window)", false, null)
    ]
    public void GetHintShouldResolveTheSentenceToAnnounce(string? target, string? hint, bool suppressed, string? expected)
    {
        Assert.AreEqual(expected, BitNewTabUtils.GetHint(target, hint, suppressed));
    }

    [TestMethod]
    public void DefaultHintShouldBeTheEnglishSentence()
    {
        Assert.AreEqual("(opens in a new tab)", BitNewTabUtils.DefaultHint);
    }

    [TestMethod]
    public void PlaceHintShouldPutTheSentenceInsideAnAnchorNamedByItsContent()
    {
        var placement = BitNewTabUtils.PlaceHint("(new tab)", null, null, "x-nth");

        Assert.AreEqual(new BitNewTabHintPlacement(null, null, "(new tab)", null), placement);
    }

    [TestMethod]
    public void PlaceHintShouldAppendTheSentenceToAnAriaLabel()
    {
        // An aria-label replaces the content, so hidden text inside the anchor would never be read.
        var placement = BitNewTabUtils.PlaceHint("(new tab)", "Docs", null, "x-nth");

        Assert.AreEqual(new BitNewTabHintPlacement("Docs (new tab)", null, null, null), placement);
    }

    [TestMethod]
    public void PlaceHintShouldPointAnAriaLabelledByAtTheSentence()
    {
        // An aria-labelledby wins over an aria-label, and it points at elements rather than holding text, so
        // the sentence becomes an element of its own that the list points at - the aria-label is left alone.
        var placement = BitNewTabUtils.PlaceHint("(new tab)", "Docs", "heading", "x-nth");

        Assert.AreEqual(new BitNewTabHintPlacement("Docs", "heading x-nth", "(new tab)", "x-nth"), placement);
    }

    [TestMethod,
        DataRow(null),
        DataRow("")
    ]
    public void PlaceHintShouldLeaveTheNameAloneWithNothingToAnnounce(string? hint)
    {
        var placement = BitNewTabUtils.PlaceHint(hint, "Docs", "heading", "x-nth");

        Assert.AreEqual(new BitNewTabHintPlacement("Docs", "heading", null, null), placement);
    }
}
