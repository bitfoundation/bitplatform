using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.VirtualKeyboard;

[TestClass]
public class BitVirtualKeyboardShorthandTests
{
    private static BitVirtualKeyboardKey One(string token) => BitVirtualKeyboardShorthand.ParseRow(token).Keys.Single();

    [TestMethod]
    public void ARowShouldBeSplitIntoOneKeyPerSpaceSeparatedToken()
    {
        var row = BitVirtualKeyboardShorthand.ParseRow("  q w   e ");

        CollectionAssert.AreEqual(new[] { "q", "w", "e" }, row.Keys.Select(k => k.Output).ToArray());
    }

    [TestMethod]
    [DataRow("{shift}", "shift", null)]
    [DataRow("{shift*1.5}", "shift", 1.5)]
    [DataRow("{gap*0.5}", "gap", 0.5)]
    [DataRow("{iso-enter*1.25}", "iso-enter", 1.25)]
    public void ANamedKeyShouldBeAReferenceWithItsWidth(string token, string id, double? width)
    {
        var key = One(token);

        Assert.AreEqual(id, key.Id);
        Assert.AreEqual(width, key.Width);
        Assert.IsNull(key.Output);
    }

    [TestMethod]
    public void TheArgumentOfALayerDeadOrCommandKeyShouldBeItsLayerMarkerOrCommand()
    {
        var layer = One("{layer:symbols*1.5}");
        Assert.AreEqual("layer:symbols", layer.Id);
        Assert.AreEqual("symbols", layer.Layer);
        Assert.AreEqual(1.5, layer.Width);

        Assert.AreEqual("acute", One("{dead:acute}").DeadKey);
        Assert.AreEqual("qty", One("{cmd:qty}").Command);
        Assert.AreEqual("foo:bar", One("{foo:bar}").Id);
    }

    [TestMethod]
    [DataRow("{Shift}")]
    [DataRow("{2}")]
    [DataRow("{shift}*2")]
    [DataRow("{*2")]
    [DataRow("{layer:sym bols}")]
    [DataRow("{shift*.5}")]
    [DataRow("{layer:}")]
    [DataRow("ab}")]
    public void AMalformedNamedKeyShouldBeAnErrorNeverALiteral(string row)
    {
        Assert.ThrowsExactly<FormatException>(() => BitVirtualKeyboardShorthand.ParseRow(row));
    }

    [TestMethod]
    [DataRow("{")]
    [DataRow("}")]
    [DataRow("{}")]
    [DataRow("*")]
    [DataRow(":")]
    [DataRow("`")]
    [DataRow(".com")]
    [DataRow("www.")]
    [DataRow("لا")]
    [DataRow("a*b")]
    public void AnyOtherTokenShouldTypeItself(string token)
    {
        var key = One(token);

        Assert.AreEqual(token, key.Output);
        Assert.IsNull(key.Id);
        Assert.IsNull(key.Width);
    }

    [TestMethod]
    [DataRow("0*2", "0", 2)]
    [DataRow("**2", "*", 2)]
    [DataRow("1.5*2", "1.5", 2)]
    [DataRow("|*1.5", "|", 1.5)]
    [DataRow(@"\\*1.5", @"\", 1.5)]
    [DataRow(@"\{*2", "{", 2)]
    public void AWidthSuffixShouldSetTheWidthOfALiteral(string token, string output, double width)
    {
        var key = One(token);

        Assert.AreEqual(output, key.Output);
        Assert.AreEqual(width, key.Width);
    }

    [TestMethod]
    public void TheEscapesShouldTypeWhatTheyStandFor()
    {
        Assert.AreEqual(@"\", One(@"\\").Output);
        Assert.AreEqual(@"\", One(@"\").Output);
        Assert.AreEqual(" ", One(@"\s").Output);
        Assert.AreEqual("{shift}", One(@"\{shift}").Output);
        Assert.AreEqual("\U0000200C", One(@"\u{200C}").Output);
        Assert.AreEqual("\U0001F600", One(@"\u{1F600}").Output);
        Assert.AreEqual("e\U00000301", One(@"\u{65}\u{301}").Output);
    }

    [TestMethod]
    [DataRow(@"\u{}")]
    [DataRow(@"\u{110000}")]
    [DataRow(@"\u{D800}")]
    [DataRow(@"\u{12G}")]
    public void AnEscapeThatIsNotACodePointShouldBeAnError(string token)
    {
        Assert.ThrowsExactly<FormatException>(() => BitVirtualKeyboardShorthand.ParseRow(token));
    }

    [TestMethod]
    public void ACombiningMarkShouldBeTypedAsWritten()
    {
        Assert.AreEqual("\U0000064E", One(@"\u{64E}").Output);
    }

    [TestMethod]
    public void AStringShouldConvertToARowAndRowsShouldTakeCollectionExpressionsOfStrings()
    {
        BitVirtualKeyboardLayer layer = new() { Id = "base", Rows = ["q w e", "{shift} z {bksp}"] };

        Assert.AreEqual(2, layer.Rows.Count);
        Assert.AreEqual("bksp", layer.Rows[1].Keys[2].Id);
    }
}