using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

/// <summary>
/// BitPdfHtmlRenderer replaces two per-op set lookups with single range compares
/// over deliberately contiguous <see cref="BitPdfOpCode"/> blocks: the Type3 `d1`
/// colour lock (FillGray..StrokeColorN) and the pending painted-line flush guard
/// (BeginText..NextLineShowTextSpacing). These tests pin each block to the exact
/// keyword set the predicate stands in for, so reordering the enum or inserting a
/// member into a block fails here instead of silently changing renderer behavior.
/// (BitPdfOpCode.cs is compile-linked into this project; Extras internals are not
/// visible to the test assembly.)
/// </summary>
[TestClass]
public class BitPdfOpCodeTests
{
    // Every operator keyword BitPdfOpCodes.Resolve maps — kept in sync with Resolve.
    private static readonly string[] AllKeywords =
    [
        "q", "Q", "cm", "w", "d", "J", "j", "M", "ri", "i", "gs",
        "g", "G", "rg", "RG", "k", "K", "cs", "CS", "sc", "scn", "SC", "SCN",
        "m", "l", "c", "v", "y", "re", "h",
        "S", "s", "f", "F", "f*", "B", "B*", "b", "b*", "n", "W", "W*",
        "sh", "Do", "INLINE_IMAGE",
        "d0", "d1",
        "BDC", "BMC", "EMC",
        "BT", "ET", "Tc", "Tw", "Tz", "TL", "Ts", "Tr", "Tf",
        "Td", "TD", "Tm", "T*", "Tj", "TJ", "'", "\"",
    ];

    // The colour-setting operators suppressed inside a Type3 `d1` glyph
    // (the renderer's former IsColorOperator set).
    private static readonly string[] ColorKeywords =
    [
        "g", "G", "rg", "RG", "k", "K", "cs", "CS", "sc", "scn", "SC", "SCN",
    ];

    // The operators a pending coalesced painted line may survive across
    // (the renderer's former TextOnlyOperators set).
    private static readonly string[] TextKeywords =
    [
        "BT", "ET", "Tj", "TJ", "'", "\"", "Td", "TD", "Tm", "T*",
        "Tf", "Tc", "Tw", "Tz", "TL", "Ts", "Tr",
    ];

    [TestMethod]
    public void ColorBlockShouldContainExactlyTheColorOperators()
    {
        foreach (string keyword in AllKeywords)
        {
            BitPdfOpCode code = BitPdfOpCodes.Resolve(keyword);
            bool inRange = code is >= BitPdfOpCode.FillGray and <= BitPdfOpCode.StrokeColorN;

            Assert.AreEqual(ColorKeywords.Contains(keyword), inRange,
                $"'{keyword}' resolved to {code}, which is on the wrong side of the FillGray..StrokeColorN colour block.");
        }
    }

    [TestMethod]
    public void TextBlockShouldContainExactlyTheTextOperators()
    {
        foreach (string keyword in AllKeywords)
        {
            BitPdfOpCode code = BitPdfOpCodes.Resolve(keyword);
            bool inRange = code is >= BitPdfOpCode.BeginText and <= BitPdfOpCode.NextLineShowTextSpacing;

            Assert.AreEqual(TextKeywords.Contains(keyword), inRange,
                $"'{keyword}' resolved to {code}, which is on the wrong side of the BeginText..NextLineShowTextSpacing text block.");
        }
    }

    [TestMethod]
    public void RangeBlocksShouldContainNoUnmappedMembers()
    {
        // A member inserted into a block would satisfy the renderer's range compare
        // without being one of the operators the predicate stands in for — even a
        // brand-new keyword AllKeywords doesn't know about yet gets caught here.
        var colorCodes = ColorKeywords.Select(BitPdfOpCodes.Resolve).ToHashSet();
        var textCodes = TextKeywords.Select(BitPdfOpCodes.Resolve).ToHashSet();

        foreach (BitPdfOpCode code in Enum.GetValues<BitPdfOpCode>())
        {
            if (code is >= BitPdfOpCode.FillGray and <= BitPdfOpCode.StrokeColorN)
            {
                Assert.IsTrue(colorCodes.Contains(code),
                    $"{code} sits inside the FillGray..StrokeColorN colour block but no colour keyword resolves to it.");
            }

            if (code is >= BitPdfOpCode.BeginText and <= BitPdfOpCode.NextLineShowTextSpacing)
            {
                Assert.IsTrue(textCodes.Contains(code),
                    $"{code} sits inside the BeginText..NextLineShowTextSpacing text block but no text keyword resolves to it.");
            }
        }
    }

    [TestMethod]
    public void EveryOperatorKeywordShouldResolveAndUnknownStringsShouldNot()
    {
        foreach (string keyword in AllKeywords)
        {
            Assert.AreNotEqual(BitPdfOpCode.Unknown, BitPdfOpCodes.Resolve(keyword),
                $"Operator keyword '{keyword}' no longer resolves to an opcode.");
        }

        Assert.AreEqual(BitPdfOpCode.Unknown, BitPdfOpCodes.Resolve("obj"));
        Assert.AreEqual(BitPdfOpCode.Unknown, BitPdfOpCodes.Resolve("endstream"));
        Assert.AreEqual(BitPdfOpCode.Unknown, BitPdfOpCodes.Resolve(""));
    }
}
