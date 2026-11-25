using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Bit.BlazorUI.Tests.Components.Extras.VirtualKeyboard.BitVirtualKeyboardTestLayouts;

namespace Bit.BlazorUI.Tests.Components.Extras.VirtualKeyboard;

/// <summary>
/// The built-in pads and language layouts against their design (PLAN.md 8.1-8.5) and their key data (layout-data.md).
/// </summary>
[TestClass]
public class BitVirtualKeyboardLayoutDataTests
{
    private static string Row(BitVirtualKeyboardCompiledLayer layer, int row) =>
        string.Join(" ", layer.Keys.Where(k => k.Row == row && k.Gap is false && k.Blank is false && k.Action == BitVirtualKeyboardKeyAction.Character).Select(k => k.Output));

    [TestMethod]
    public void EveryRowOfEveryPadShouldFillItsGrid()
    {
        foreach (var id in BitVirtualKeyboardLayouts.Ids.Where(id => id.StartsWith("x-")))
        {
            foreach (var mode in new[] { BitVirtualKeyboardMode.Inline, BitVirtualKeyboardMode.Docked })
            {
                var compiled = Compile(BitVirtualKeyboardLayouts.Get(id)!, BitVirtualKeyboardLayouts.Get(id)!.Id switch { _ => BitVirtualKeyboardPurpose.Text }, mode);
                AssertNoOverlap(compiled);
                foreach (var layer in compiled.Layers)
                {
                    CollectionAssert.AreEqual(Enumerable.Repeat(compiled.Columns, layer.Rows).ToArray(), RowCoverage(layer), $"{id} {mode} {layer.Id}");
                }
            }
        }
    }

    [TestMethod]
    public void EveryRowOfEverySymbolLayerShouldBeTenUnits()
    {
        foreach (var id in BitVirtualKeyboardLayouts.Ids.Where(id => id.EndsWith("-compact")))
        {
            var compiled = Compile(BitVirtualKeyboardLayouts.Get(id)!, languages: 2);
            foreach (var layerId in new[] { "symbols", "symbols2" })
            {
                var coverage = RowCoverage(compiled.GetLayer(layerId)!);
                CollectionAssert.AreEqual(new[] { 40, 40, 40 }, coverage.Take(3).ToArray(), $"{id} {layerId}");
                Assert.AreEqual(compiled.Columns, coverage[3], $"{id} {layerId}: the bottom row stretches its space bar");
            }
        }
    }

    [TestMethod]
    public void ForPurposeShouldGiveThePadOfEachPurpose()
    {
        Assert.AreEqual("x-phone", BitVirtualKeyboardLayouts.ForPurpose(BitVirtualKeyboardPurpose.Tel).Id);
        Assert.AreEqual("x-abc", BitVirtualKeyboardLayouts.ForPurpose(BitVirtualKeyboardPurpose.Alphabetic).Id);
        Assert.AreEqual("x-pos", BitVirtualKeyboardLayouts.ForPurpose(BitVirtualKeyboardPurpose.Pos).Id);
        Assert.AreEqual("en", BitVirtualKeyboardLayouts.ForPurpose(BitVirtualKeyboardPurpose.Email).Id);
        Assert.AreEqual("fr-t-k0-windows", BitVirtualKeyboardLayouts.ForCulture(CultureInfo.GetCultureInfo("fr-FR"), BitVirtualKeyboardForm.Full)!.Id);
        Assert.AreEqual("en-GB", BitVirtualKeyboardLayouts.ForCulture(CultureInfo.GetCultureInfo("en-GB"), BitVirtualKeyboardForm.Full)!.Id);
        Assert.AreEqual("fa-compact", BitVirtualKeyboardLayouts.ForCulture(CultureInfo.GetCultureInfo("fa-IR"), BitVirtualKeyboardForm.Compact)!.Id);
    }

    [TestMethod]
    public void ThePhonePadShouldCarryTheLettersOfE161()
    {
        var layer = Compile(BitVirtualKeyboardLayouts.Phone).Layers[0];

        Assert.AreEqual("1 2 3 4 5 6 7 8 9 * 0 #", string.Join(" ", layer.Keys.Where(k => k.Gap is false && k.Action == BitVirtualKeyboardKeyAction.Character).Select(k => k.Output)));
        Assert.AreEqual("ABC", Key(layer, "2").Hint);
        Assert.AreEqual("PQRS", Key(layer, "7").Hint);
        Assert.AreEqual("WXYZ", Key(layer, "9").Hint);
        CollectionAssert.AreEqual(new[] { "+" }, Key(layer, "0").LongPress.ToArray());
        Assert.AreEqual("bit-vkb-nub", Key(layer, "5").Class);
    }

    [TestMethod]
    public void TheCalculatorShouldTypeAsciiOperatorsAndShowTheirSigns()
    {
        var layer = Compile(BitVirtualKeyboardLayouts.Calculator).Layers[0];

        Assert.AreEqual("×", Key(layer, "*").Label);
        Assert.AreEqual("*", Key(layer, "*").Output);
        Assert.AreEqual("−", Key(layer, "-").Label);
    }

    [TestMethod]
    public void ThePersianLayoutShouldHoldIsiriWithPersianLetters()
    {
        var compiled = Compile(BitVirtualKeyboardLayouts.Persian);
        var baseLayer = compiled.GetLayer("base")!;

        Assert.AreEqual("ش س ی ب ل ا ت ن م ک گ", Row(baseLayer, 2));
        Assert.AreEqual("ض ص ث ق ف غ ع ه خ ح ج چ \\", Row(baseLayer, 1));
        Assert.IsFalse(Row(baseLayer, 2).Contains('\x064A') || Row(baseLayer, 2).Contains('\x0643'), "Persian ی and ک, never Arabic ي and ك");
        Assert.AreEqual(BitDir.Rtl, compiled.Direction);
        Assert.AreEqual("÷ ! ٬ ٫ ﷼ ٪ × ، * ) ( ـ +", Row(compiled.GetLayer("shift")!, 0));
        Assert.AreEqual("\x200C", compiled.GetLayer("shift")!.Keys.Single(k => k.KeyId == "zwnj").Output);

        var compact = Compile(BitVirtualKeyboardLayouts.PersianCompact);
        Assert.IsTrue(compact.GetLayer("base")!.Keys.Any(k => k.Output == "\x200C"), "Persian Compact has a ZWNJ key");
        Assert.IsNull(compact.GetLayer("shift"), "no case, no shift layer");
        Assert.AreEqual("،", compact.GetLayer("base")!.Keys.Single(k => k.KeyId == "comma").Output);
        Assert.AreEqual("۱", compact.GetLayer("symbols")!.Keys[0].Output);
    }

    [TestMethod]
    public void TheArabicLayoutShouldHoldArabic101()
    {
        var compiled = Compile(BitVirtualKeyboardLayouts.Arabic);

        Assert.AreEqual("ش س ي ب ل ا ت ن م ك ط", Row(compiled.GetLayer("base")!, 2));
        Assert.AreEqual("ئ ء ؤ ر لا ى ة و ز ظ", Row(compiled.GetLayer("base")!, 3));
        Assert.AreEqual("\x25CC\x0651", compiled.GetLayer("shift")!.Keys[0].Label, "a harakat is labeled on a dotted circle");

        var compact = Compile(BitVirtualKeyboardLayouts.Get("ar-compact")!);
        Assert.IsTrue(compact.GetLayer("base")!.Keys.Any(k => k.Output == "ذ"), "Arabic Compact keeps the ذ of row E");
        Assert.AreEqual("١", compact.GetLayer("symbols")!.Keys[0].Output);
    }

    [TestMethod]
    public void TheHebrewLayoutShouldSwapItsParentheses()
    {
        var compiled = Compile(BitVirtualKeyboardLayouts.Hebrew);

        Assert.AreEqual("~ ! @ # $ % ^ & * ) ( _ +", Row(compiled.GetLayer("shift")!, 0));
        Assert.AreEqual("ש ד ג כ ע י ח ל ך ף ,", Row(compiled.GetLayer("base")!, 2));
    }

    [TestMethod]
    public void TheGermanLayoutShouldHaveDeadKeysAndAnAltGrLayer()
    {
        var compiled = Compile(BitVirtualKeyboardLayouts.German, languages: 2);
        var baseLayer = compiled.GetLayer("base")!;

        Assert.AreEqual("dead:circumflex", baseLayer.Keys[0].KeyId);
        Assert.AreEqual(BitVirtualKeyboardKeyAction.DeadKey, baseLayer.Keys[0].Action);
        Assert.AreEqual("^", baseLayer.Keys[0].Label);
        Assert.AreEqual("ê", compiled.DeadKeys["circumflex"]["e"]);
        Assert.AreEqual("altgr", compiled.AltGrLayer);
        Assert.IsTrue(compiled.GetLayer("altgr")!.Keys.Any(k => k.Output == "@"));
        Assert.AreEqual("q w e r t z u i o p ü +", Row(baseLayer, 1));
        Assert.AreEqual("a s d f g h j k l ö ä #", Row(baseLayer, 2), "ISO: the scan-2B key ends row C");
        Assert.IsTrue(baseLayer.Keys.Any(k => k.KeyId == "iso-enter" && k.RowSpan == 2));
    }

    [TestMethod]
    public void TheTurkishLayoutShouldCaseItsDottedAndDotlessI()
    {
        var compiled = Compile(BitVirtualKeyboardLayouts.Turkish);

        Assert.AreEqual("A S D F G H J K L Ş İ ;", Row(compiled.GetLayer("shift")!, 2), "the ISO key of row C: a comma, a semicolon shifted");
        Assert.AreEqual("î", compiled.DeadKeys["circumflex"]["\x0131"]);

        var compact = Compile(BitVirtualKeyboardLayouts.Get("tr-compact")!, culture: "tr-TR");
        Assert.AreEqual("İ", compact.GetLayer("shift")!.Keys.Single(k => k.Row == 1 && k.Column == compact.GetLayer("base")!.Keys.Single(b => b.Output == "i").Column).Output);
    }

    [TestMethod]
    public void TheFrenchAndSpanishLayoutsShouldHoldTheirRows()
    {
        var french = Compile(BitVirtualKeyboardLayouts.French).GetLayer("base")!;
        Assert.AreEqual("² & é \" ' ( - è _ ç à ) =", Row(french, 0));
        Assert.AreEqual("< w x c v b n , ; : !", Row(french, 3));

        var spanish = Compile(BitVirtualKeyboardLayouts.Spanish).GetLayer("base")!;
        Assert.AreEqual("a s d f g h j k l ñ ç", Row(spanish, 2));
    }
}