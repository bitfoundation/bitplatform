using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils;

[TestClass]
public sealed class BitShortIdTests
{
    [TestMethod]
    public void BitShortIdShouldStartWithALetter()
    {
        // The ids are rendered as DOM ids and read back by CSS selectors as well as by aria-* references,
        // and a selector cannot start with a digit without being escaped.
        for (var i = 0; i < 10_000; i++)
        {
            var id = BitShortId.NewId();

            Assert.IsTrue(id.Length > 0);
            Assert.IsTrue(char.IsAsciiLetterLower(id[0]), $"'{id}' does not start with a letter.");
        }
    }

    [TestMethod]
    public void BitShortIdShouldOnlyUseLowercaseLettersAndDigits()
    {
        for (var i = 0; i < 10_000; i++)
        {
            var id = BitShortId.NewId();

            Assert.IsTrue(id.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c)), $"'{id}' is not alphanumeric.");
        }
    }

    [TestMethod]
    public void BitShortIdShouldNotRepeatItself()
    {
        var ids = new HashSet<string>();

        for (var i = 0; i < 10_000; i++)
        {
            ids.Add(BitShortId.NewId());
        }

        // a birthday collision over 10k draws of a 32-bit value is possible but vanishingly rare
        Assert.IsTrue(ids.Count >= 9_999, $"{10_000 - ids.Count} ids collided.");
    }

    [TestMethod]
    [DataRow(0, "a")]
    [DataRow(1, "b")]
    [DataRow(25, "z")]
    [DataRow(26, "0")]
    [DataRow(35, "9")]
    [DataRow(36, "ba")]
    [DataRow(71, "b9")]
    [DataRow(1295, "99")]
    public void BitShortIdShouldRenderTheValueInBase36(int value, string expected)
    {
        Assert.AreEqual(expected, BitShortId.ToString(value));
    }
}
