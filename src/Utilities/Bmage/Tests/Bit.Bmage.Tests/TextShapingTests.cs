using System.Numerics;
using Bit.Bmage.Drawing;
using Bit.Bmage.Text;
using Bit.Bmage.Text.Unicode;

namespace Bit.Bmage.Tests;

/// <summary>
/// Shaping and ordering of right-to-left and mixed text with a real Persian font (Vazirmatn, SIL Open Font License).
/// Joining forms are checked against the glyphs the font maps to the Unicode presentation forms of the same letters,
/// which is how the font's own designers say each form looks. The font has no isol feature: a letter on its own is
/// its nominal glyph.
/// </summary>
[TestClass]
public sealed class TextShapingTests
{
    private static readonly FontFile Vazirmatn = FontFile.Load(File.ReadAllBytes("Assets/Vazirmatn-Regular.ttf"));
    private static readonly FontFile Latin = FontFile.Load(File.ReadAllBytes("Assets/BmageTest.ttf"));

    private static ushort[] Shape(string text, FontFile? font = null)
    {
        var codePoints = TextShaper.CodePoints(text);
        var buffer = TextShaper.ShapeRun(font ?? Vazirmatn, codePoints, 0, codePoints.Length, UnicodeScript.Arabic, rtl: true);
        return [.. buffer.AsSpan().ToArray().Where(g => !g.Hidden).Select(g => g.Glyph)];
    }

    private static ushort Nominal(string letter) => Vazirmatn.GetGlyphIndex(char.ConvertToUtf32(letter, 0));

    private static ushort Form(int presentationForm)
    {
        var glyph = Vazirmatn.GetGlyphIndex(presentationForm);
        Assert.AreNotEqual(0, glyph, $"the font maps U+{presentationForm:X4}");
        return glyph;
    }

    [TestMethod]
    [DataRow("ب", 0xFE8F, 0xFE90, 0xFE91, 0xFE92, DisplayName = "beh")]
    [DataRow("پ", 0xFB56, 0xFB57, 0xFB58, 0xFB59, DisplayName = "peh")]
    [DataRow("چ", 0xFB7A, 0xFB7B, 0xFB7C, 0xFB7D, DisplayName = "tcheh")]
    [DataRow("گ", 0xFB92, 0xFB93, 0xFB94, 0xFB95, DisplayName = "gaf")]
    [DataRow("ک", 0xFB8E, 0xFB8F, 0xFB90, 0xFB91, DisplayName = "keheh")]
    [DataRow("ی", 0xFBFC, 0xFBFD, 0xFBFE, 0xFBFF, DisplayName = "farsi yeh")]
    [DataRow("س", 0xFEB1, 0xFEB2, 0xFEB3, 0xFEB4, DisplayName = "seen")]
    [DataRow("ع", 0xFEC9, 0xFECA, 0xFECB, 0xFECC, DisplayName = "ain")]
    [DataRow("ه", 0xFEE9, 0xFEEA, 0xFEEB, 0xFEEC, DisplayName = "heh")]
    public void ADualJoiningLetterTakesItsFourForms(string letter, int isolated, int final, int initial, int medial)
    {
        Assert.AreNotEqual(0, Form(isolated));
        CollectionAssert.AreEqual(new[] { Nominal(letter) }, Shape(letter));
        // Next to alef, which joins backward only and asks no contextual alternate of its neighbours: the letter
        // before it starts a join, after a beh it ends one, between the two it continues one.
        Assert.AreEqual(Form(initial), Shape(letter + "ا")[0]);
        Assert.AreEqual(Form(final), Shape("ب" + letter)[1]);
        Assert.AreEqual(Form(medial), Shape("ب" + letter + "ا")[1]);
    }

    [TestMethod]
    public void ContextualAlternatesApply()
    {
        // Vazirmatn lengthens an initial peh before another dotted letter (its calt feature), so the dots stay clear.
        var plain = Form(0xFB58);
        var initial = Shape("پپپ")[0];
        Assert.AreNotEqual(plain, initial);
        Assert.AreEqual(plain + 1, initial, "uniFB58.long1, the glyph after the plain initial peh");
    }

    [TestMethod]
    public void ARightJoiningLetterEndsTheJoin()
    {
        // Beh then alef: alef joins the beh before it, and nothing after the alef joins it.
        CollectionAssert.AreEqual(new[] { Form(0xFE91), Form(0xFE8E), Nominal("ب") }, Shape("باب"));
        // Dal, reh and waw never join forward: each starts the next letter over.
        CollectionAssert.AreEqual(new[] { Form(0xFE91), Form(0xFEAA), Form(0xFE91), Form(0xFEAE) }, Shape("بدبر"));
    }

    [TestMethod]
    public void ZeroWidthNonJoinerSeparatesLettersAndDrawsNothing()
    {
        // The Persian half-space: two isolated behs, and the ZWNJ itself takes no room.
        var codePoints = TextShaper.CodePoints("ب‌ب");
        var buffer = TextShaper.ShapeRun(Vazirmatn, codePoints, 0, codePoints.Length, UnicodeScript.Arabic, rtl: true);
        var glyphs = buffer.AsSpan().ToArray();
        CollectionAssert.AreEqual(new[] { Nominal("ب"), Nominal("ب") }, glyphs.Where(g => g.XAdvance > 0).Select(g => g.Glyph).ToArray());
        Assert.AreEqual(3, glyphs.Length);
        Assert.AreEqual(0, glyphs[1].XAdvance, "ZWNJ has no width");

        // A zero width joiner joins instead.
        Assert.AreEqual(Form(0xFE91), Shape("ب‍")[0]);
    }

    [TestMethod]
    public void InvisibleCharactersAreNeverDrawnEvenWhenTheFontHasAGlyph()
    {
        // A soft hyphen, a right-to-left mark and a word joiner between two letters: only the letters are drawn.
        var glyphs = Line("A­‏B⁠C", Latin);
        Assert.HasCount(3, glyphs);
        CollectionAssert.AreEqual(new[] { Latin.GetGlyphIndex('A'), Latin.GetGlyphIndex('B'), Latin.GetGlyphIndex('C') }, glyphs.Select(g => g.Glyph).ToArray());
    }

    [TestMethod]
    public void LamAndAlefFormALigature()
    {
        CollectionAssert.AreEqual(new[] { Form(0xFEFB) }, Shape("لا"));
        // After a joining letter the ligature takes its final form.
        CollectionAssert.AreEqual(new[] { Form(0xFE91), Form(0xFEFC) }, Shape("بلا"));
    }

    [TestMethod]
    public void HarakatHangOnTheirLetters()
    {
        // Beh with a fatha: the mark takes no room and sits above its letter.
        var codePoints = TextShaper.CodePoints("بَ");
        var line = TextShaper.LayoutLine([Vazirmatn], codePoints, BidiParagraph.Create(codePoints).GetLineLevels(0, codePoints.Length), 0, codePoints.Length, 100, 0);
        Assert.HasCount(2, line.Glyphs);
        var beh = line.Glyphs.Single(g => g.Glyph == Nominal("ب"));
        var fatha = line.Glyphs.Single(g => g.Glyph != beh.Glyph);
        Assert.AreEqual(Vazirmatn.GetAdvanceWidth(beh.Glyph) * 100f / Vazirmatn.UnitsPerEm, line.Width, 0.01f);
        var behBounds = Bounds(beh);
        var fathaBounds = Bounds(fatha);
        Assert.IsLessThan(behBounds.MinY, fathaBounds.MaxY + 1, "the fatha is above the beh");
        Assert.IsTrue(fathaBounds.MinX >= behBounds.MinX - 1 && fathaBounds.MaxX <= behBounds.MaxX + 1, "the fatha is over the beh");
    }

    private static (float MinX, float MinY, float MaxX, float MaxY) Bounds(PlacedGlyph glyph)
    {
        var builder = new PathBuilder();
        glyph.Font.GetGlyphOutline(glyph.Glyph)!.AppendTo(builder, new Vector2(glyph.X, glyph.Y), 100f / glyph.Font.UnitsPerEm);
        var points = builder.Build().Contours.SelectMany(c => c).ToArray();
        return (points.Min(p => p.X), points.Min(p => p.Y), points.Max(p => p.X), points.Max(p => p.Y));
    }

    private static PlacedGlyph[] Line(string text, params FontFile[] fonts)
    {
        var codePoints = TextShaper.CodePoints(text);
        var bidi = BidiParagraph.Create(codePoints);
        return TextShaper.LayoutLine(fonts.Length == 0 ? [Vazirmatn] : fonts, codePoints, bidi.GetLineLevels(0, codePoints.Length), 0, codePoints.Length, 100, 0).Glyphs;
    }

    [TestMethod]
    public void RightToLeftTextRunsFromRightToLeft()
    {
        // "بد": beh first in the text, so rightmost on the line.
        var glyphs = Line("بد");
        Assert.HasCount(2, glyphs);
        var beh = glyphs.Single(g => g.Glyph == Form(0xFE91));
        var dal = glyphs.Single(g => g.Glyph == Form(0xFEAA));
        Assert.IsGreaterThan(dal.X, beh.X);
    }

    [TestMethod]
    public void NumbersReadLeftToRightInsideRightToLeftText()
    {
        var one = Vazirmatn.GetGlyphIndex('۱');
        var two = Vazirmatn.GetGlyphIndex('۲');
        var three = Vazirmatn.GetGlyphIndex('۳');
        var glyphs = Line("عدد ۱۲۳");
        var x1 = glyphs.Single(g => g.Glyph == one).X;
        var x2 = glyphs.Single(g => g.Glyph == two).X;
        var x3 = glyphs.Single(g => g.Glyph == three).X;
        Assert.IsTrue(x1 < x2 && x2 < x3, "the digits keep their order");
        // And the number, last in the text, is leftmost.
        Assert.IsLessThan(glyphs.Where(g => g.Glyph != one && g.Glyph != two && g.Glyph != three && Vazirmatn.GetGlyphOutline(g.Glyph) is not null).Min(g => g.X), x3);
    }

    [TestMethod]
    public void BracketsMirrorInRightToLeftText()
    {
        // In a right-to-left paragraph the opening bracket is on the right, so it is drawn as the closing shape.
        var glyphs = Line("(ب)");
        var ordered = glyphs.OrderBy(g => g.X).Select(g => g.Glyph).ToArray();
        Assert.AreEqual(Vazirmatn.GetGlyphIndex('('), ordered[0]);
        Assert.AreEqual(Vazirmatn.GetGlyphIndex(')'), ordered[^1]);
    }

    [TestMethod]
    public void ALatinWordInAPersianSentenceKeepsItsOwnOrder()
    {
        // A right-to-left paragraph: the Persian word first in the text is on the right, the Latin word on the left,
        // and the Latin letters still read left to right.
        var glyphs = Line("سلام ABC", Latin, Vazirmatn);
        var a = glyphs.Single(g => g.Font == Latin && g.Glyph == Latin.GetGlyphIndex('A'));
        var b = glyphs.Single(g => g.Font == Latin && g.Glyph == Latin.GetGlyphIndex('B'));
        var c = glyphs.Single(g => g.Font == Latin && g.Glyph == Latin.GetGlyphIndex('C'));
        Assert.IsTrue(a.X < b.X && b.X < c.X);
        Assert.IsGreaterThan(c.X, glyphs.Where(g => g.Font == Vazirmatn).Min(g => g.X));
    }

    [TestMethod]
    public void EachCharacterIsDrawnWithAFontThatHasIt()
    {
        // The test font has only A, B and C; the Persian word comes from the next registered font.
        var rasterizer = new FontTextRasterizer().Register("Test", Latin).Register("Vazirmatn", Vazirmatn);
        var style = new TextStyle { FontSize = 40 };
        var path = rasterizer.GetTextPath("AB سلام", new Vector2(10, 50), style);
        Assert.IsNotNull(path);
        var glyphs = Line("AB سلام", Latin, Vazirmatn);
        Assert.AreEqual(2, glyphs.Count(g => g.Font == Latin));
        Assert.IsTrue(glyphs.Where(g => g.Font == Vazirmatn).All(g => g.Glyph != 0), "no Persian letter falls back to .notdef");
        Assert.IsGreaterThan(rasterizer.Measure("AB", style).Width, rasterizer.Measure("AB سلام", style).Width);
    }

    [TestMethod]
    public void PersianTextWrapsIntoLines()
    {
        var rasterizer = new FontTextRasterizer().Register("Vazirmatn", Vazirmatn);
        var style = new TextStyle { FontSize = 30, MaxWidth = 160 };
        var metrics = rasterizer.Measure("این یک متن فارسی است که باید در چند خط شکسته شود", style);
        Assert.IsGreaterThan(1, metrics.LineCount);
        Assert.IsLessThanOrEqualTo(160f, metrics.Width);
    }

    [TestMethod]
    public void ShapingAHostileStringNeitherThrowsNorHangs()
    {
        // Marks with nothing to sit on, controls, lone joiners, deep embeddings and every bracket at once.
        var text = "ََ‍‌" + new string('‫', 200) + "((([[[{{{ب" + new string('‬', 300) + "⁧لا⁩" + "﻿؜";
        var rasterizer = new FontTextRasterizer().Register("Vazirmatn", Vazirmatn);
        _ = rasterizer.GetTextPath(text, Vector2.Zero, new TextStyle { FontSize = 20, MaxWidth = 50 });
        _ = rasterizer.Measure(text, new TextStyle { FontSize = 20 });
    }
}
