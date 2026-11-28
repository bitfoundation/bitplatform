using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Bit.BlazorUI.Tests.Components.Extras.VirtualKeyboard.BitVirtualKeyboardTestLayouts;

namespace Bit.BlazorUI.Tests.Components.Extras.VirtualKeyboard;

[TestClass]
public class BitVirtualKeyboardLayoutCompilerTests
{
    // ---------------------------------------------------------------- merging

    [TestMethod]
    public void ANamedKeyShouldTakeItsBuiltInValuesWithTheBagOverThemAndTheRowOverBoth()
    {
        var layout = Layout(Layer("base", "a {bksp} {enter*2}")) with
        {
            Keys = new() { ["bksp"] = new() { Width = 1.5, Hint = "del" } },
        };

        var layer = Compile(layout).Layers[0];
        var bksp = Key(layer, "bksp");
        var enter = Key(layer, "enter");

        Assert.AreEqual(BitVirtualKeyboardKeyAction.Backspace, bksp.Action, "the built-in action survives the bag entry");
        Assert.AreEqual(6, bksp.ColumnSpan, "the bag width wins over the built-in one");
        Assert.AreEqual("del", bksp.Hint);
        Assert.IsTrue(bksp.Repeat);
        Assert.AreEqual(8, enter.ColumnSpan, "the row's width wins");
        Assert.AreEqual(BitVirtualKeyboardKeyAction.Enter, enter.Action);
    }

    [TestMethod]
    public void ALiteralShouldTakeTheBagEntryOfItsText()
    {
        var layout = Layout(Layer("base", "1 2 3")) with
        {
            Keys = new() { ["2"] = new() { Hint = "ABC", LongPress = ["a", "b", "c"] } },
        };

        var two = Key(Compile(layout).Layers[0], "2");

        Assert.AreEqual("2", two.Output);
        Assert.AreEqual("ABC", two.Hint);
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, two.LongPress.ToArray());
        Assert.IsFalse(two.Repeat, "a key with alternates does not repeat");
    }

    // ---------------------------------------------------------------- validation

    [TestMethod]
    public void AMistakeInALayoutShouldBeAnArgumentExceptionNamingIt()
    {
        void Throws(BitVirtualKeyboardLayout layout, string part)
        {
            var e = Assert.ThrowsExactly<ArgumentException>(() => Compile(layout));
            StringAssert.Contains(e.Message, part);
        }

        Throws(Layout(Layer("base", "a")) with { Id = "" }, "Id");
        Throws(Layout(Layer("base", "a")) with { Name = "" }, "Name");
        Throws(Layout(), "at least one layer");
        Throws(Layout(Layer("base", "a"), Layer("base", "b")), "two layers");
        Throws(Layout(Layer("base", "a {nope}")), "{nope}");
        Throws(Layout(Layer("base", "a {layer:missing}")), "missing");
        Throws(Layout(Layer("base", "a {dead:acute}")), "acute");
        Throws(Layout(Layer("base", "a*0.3")), "0.25");
        Throws(Layout(Layer("base", "a")) with { Keys = new() { ["a"] = new() { Width = 1e-12 } } }, "0.25");
        Throws(Layout(Layer("base", "a")) with { Keys = new() { ["a"] = new() { Height = 2 } } }, "past the last row");
        Throws(Layout(new BitVirtualKeyboardLayer { Id = "base", Rows = [new() { Keys = [new() { Action = BitVirtualKeyboardKeyAction.Custom, Id = "x" }] }] })
               with { Keys = new() { ["x"] = new() { Action = BitVirtualKeyboardKeyAction.Custom } } }, "Command");
        Throws(Layout(new BitVirtualKeyboardLayer { Id = "base", Rows = [new() { Keys = [] }] }), "empty row");
    }

    // ---------------------------------------------------------------- purposes and environment

    [TestMethod]
    public void TheCommaShouldTypeAnAtSignForAnEmailAndASlashForAUrl()
    {
        var layout = Layout(Layer("base", "a {comma} {tld} {space}"));

        string Comma(BitVirtualKeyboardPurpose purpose) => Compile(layout, purpose).Layers[0].Keys.Single(k => k.KeyId == "comma").Output!;

        Assert.AreEqual(",", Comma(BitVirtualKeyboardPurpose.Text));
        Assert.AreEqual("@", Comma(BitVirtualKeyboardPurpose.Email));
        Assert.AreEqual("/", Comma(BitVirtualKeyboardPurpose.Url));
    }

    [TestMethod]
    public void AKeyForOtherPurposesShouldBeLeftOutOfALanguageLayoutAndGappedInAPad()
    {
        var language = Layout(Layer("base", "a b c d", "{tld} {space}"));
        var pad = language with { Id = "x-test" };

        Assert.IsFalse(Compile(language).Layers[0].Keys.Any(k => k.KeyId == "tld"));
        Assert.IsTrue(Compile(language, BitVirtualKeyboardPurpose.Email).Layers[0].Keys.Any(k => k.KeyId == "tld"));

        var gapped = Compile(pad).Layers[0].Keys.Single(k => k.Row == 1 && k.Column == 0);
        Assert.IsTrue(gapped.Gap);
        Assert.AreEqual(6, gapped.ColumnSpan, "the gap keeps the width of the key it stands for");
    }

    [TestMethod]
    public void HideLanguageAndAltGrKeysShouldBeDroppedWhereTheyCannotAct()
    {
        var layout = Layout(Layer("base", "a {hide} {lang} {altgr} {space}"));

        var inline = Compile(layout).Layers[0];
        Assert.IsFalse(inline.Keys.Any(k => k.BuiltIn is "hide" or "lang" or "altgr"));

        var docked = Compile(layout, mode: BitVirtualKeyboardMode.Docked, languages: 2).Layers[0];
        Assert.IsTrue(docked.Keys.Any(k => k.BuiltIn == "hide"));
        Assert.AreEqual("FA", Key(docked, "lang").Label, "the language key shows the next layout");
        Assert.IsFalse(docked.Keys.Any(k => k.BuiltIn == "altgr"), "no AltGr layer, no AltGr key");
    }

    // ---------------------------------------------------------------- derived layers

    [TestMethod]
    public void ALayoutWithAShiftKeyAndNoShiftLayerShouldGetOneUpperCasingItsLetters()
    {
        var layout = Layout(Layer("base", "a 1 .com {shift}"));

        var compiled = Compile(layout);
        var shift = compiled.GetLayer("shift")!;

        Assert.AreEqual("shift", compiled.ShiftLayer);
        CollectionAssert.AreEqual(new[] { "A", "1", ".com" }, shift.Keys.Take(3).Select(k => k.Output).ToArray());
        Assert.IsNull(Compile(layout, autoShift: false).GetLayer("shift"));
        Assert.IsNull(Compile(Layout(Layer("base", "a b"))).GetLayer("shift"), "no Shift key, no shift layer");
    }

    [TestMethod]
    public void TurkishCasingShouldGiveTheDottedCapitalI()
    {
        var layout = Layout(Layer("base", "i ı {shift}")) with { Culture = "tr-TR" };

        var shift = Compile(layout).GetLayer("shift")!;

        Assert.AreEqual("\U00000130", shift.Keys[0].Output);
        Assert.AreEqual("I", shift.Keys[1].Output);
    }

    [TestMethod]
    public void ADerivedShiftLayerShouldUpperCaseTheMultiTapItemsOfALetter()
    {
        var layout = Layout(Layer("base", "a {shift}")) with { Keys = new() { ["a"] = new() { MultiTap = ["a", "b", "c"] } } };

        var shift = Compile(layout).GetLayer("shift")!;

        Assert.AreEqual("A", shift.Keys[0].Output);
        CollectionAssert.AreEqual(new[] { "A", "B", "C" }, shift.Keys[0].MultiTap.ToArray());
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, Compile(layout).Layers[0].Keys[0].MultiTap.ToArray());
    }

    [TestMethod]
    public void TheDeadKeysOfACompiledLayoutShouldBeACopy()
    {
        var acute = new System.Collections.Generic.Dictionary<string, string> { ["e"] = "é", [" "] = "´" };
        var layout = Layout(Layer("base", "a {dead:acute} e")) with { Id = "dead-copy-test", DeadKeys = new() { ["acute"] = acute } };

        var compiled = Compile(layout);
        acute["e"] = "x";
        layout.DeadKeys["grave"] = new() { [" "] = "`" };

        Assert.AreEqual("é", compiled.DeadKeys["acute"]["e"]);
        Assert.IsFalse(compiled.DeadKeys.ContainsKey("grave"));
    }

    [TestMethod]
    public void ACapsLockKeyShouldGetACapsLayerThatLeavesDigitsAndSymbols()
    {
        var compiled = Compile(BitVirtualKeyboardLayouts.English);
        var caps = compiled.GetLayer("caps")!;

        Assert.AreEqual("caps", compiled.CapsLayer);
        Assert.IsTrue(caps.Keys.Any(k => k.Output == "Q"));
        Assert.IsTrue(caps.Keys.Any(k => k.Output == "1"), "Caps Lock leaves the digits");
        Assert.IsFalse(caps.Keys.Any(k => k.Output == "!"));
    }

    // ---------------------------------------------------------------- digits and separators

    [TestMethod]
    [DataRow("en-US", BitVirtualKeyboardDigits.Latin, ".")]
    [DataRow("de-DE", BitVirtualKeyboardDigits.Latin, ",")]
    [DataRow("fa-IR", BitVirtualKeyboardDigits.Latin, ".")]
    [DataRow("ar-SA", BitVirtualKeyboardDigits.Latin, ".")]
    [DataRow("fa-IR", BitVirtualKeyboardDigits.Persian, "\U0000066B")]
    [DataRow("ar-SA", BitVirtualKeyboardDigits.ArabicIndic, "\U0000066B")]
    public void TheDecimalKeyShouldTypeASeparatorTheDigitsParseWith(string culture, BitVirtualKeyboardDigits digits, string separator)
    {
        var layout = Layout(Layer("base", "1 {decimal}")) with { Id = "x-test" };

        Assert.AreEqual(separator, Key(Compile(layout, culture: culture, digits: digits).Layers[0], "decimal").Output);
    }

    [TestMethod]
    public void ACultureOfTheSameNameWithOtherSeparatorsShouldCompileApart()
    {
        var layout = Layout(Layer("base", "1 {decimal} {datesep}")) with { Id = "x-test" };
        var custom = (System.Globalization.CultureInfo)System.Globalization.CultureInfo.GetCultureInfo("en-US").Clone();
        custom.NumberFormat.NumberDecimalSeparator = ",";
        custom.DateTimeFormat.DateSeparator = "-";

        var english = Compile(layout).Layers[0];
        var customized = BitVirtualKeyboardLayoutCompiler.Compile(layout, new() { Culture = custom, LanguagesCount = 1 }).Layers[0];

        Assert.AreEqual(".", Key(english, "decimal").Output);
        Assert.AreEqual(",", Key(customized, "decimal").Output);
        Assert.AreEqual("-", Key(customized, "datesep").Output);
    }

    [TestMethod]
    public void ANumberTargetShouldGetAPeriodAndLatinDigits()
    {
        var layout = Layout(Layer("base", "1 {decimal}")) with { Id = "x-test" };

        var layer = Compile(layout, culture: "fa-IR", digits: BitVirtualKeyboardDigits.Persian, numberTarget: true).Layers[0];

        Assert.AreEqual("1", layer.Keys[0].Output);
        Assert.AreEqual(".", layer.Keys[1].Output);
    }

    [TestMethod]
    public void APadShouldTypeTheDigitsOfItsDigitSetAndALanguageLayoutShouldNot()
    {
        var pad = Layout(Layer("base", "1 2")) with { Id = "x-test" };

        Assert.AreEqual("\U000006F1", Compile(pad, digits: BitVirtualKeyboardDigits.Persian).Layers[0].Keys[0].Output);
        Assert.AreEqual("\U00000661", Compile(pad, digits: BitVirtualKeyboardDigits.ArabicIndic).Layers[0].Keys[0].Output);
        Assert.AreEqual("1", Compile(pad with { Id = "test" }, digits: BitVirtualKeyboardDigits.Persian).Layers[0].Keys[0].Output);
    }

    [TestMethod]
    public void AKeyTypingSeveralDigitsShouldTypeThemAllInTheDigitSet()
    {
        var layer = Compile(BitVirtualKeyboardLayouts.Pos, purpose: BitVirtualKeyboardPurpose.Pos, digits: BitVirtualKeyboardDigits.Persian).Layers[0];

        var zeros = layer.Keys.Where(k => k.Row == 3 && k.Action is BitVirtualKeyboardKeyAction.Character).Select(k => k.Output).ToArray();

        CollectionAssert.AreEqual(new[] { "\U000006F0", "\U000006F0\U000006F0", "\U000006F0\U000006F0\U000006F0" }, zeros[..3]);
        Assert.AreEqual("\U000006F0\U000006F0", layer.Keys.First(k => k.Output == zeros[1]).Label, "the keycap shows what it types");
    }

    [TestMethod]
    public void TheDateSeparatorShouldLeaveOutTheBidiMarks()
    {
        var layout = Layout(Layer("base", "1 {datesep}"));

        Assert.AreEqual("/", Key(Compile(layout, culture: "ar-SA").Layers[0], "datesep").Output);
    }

    // ---------------------------------------------------------------- labels

    [TestMethod]
    public void ALoneCombiningMarkShouldBeLabeledOnADottedCircle()
    {
        var layout = Layout(Layer("base", @"\u{64E} a"));

        Assert.AreEqual("\U000025CC\U0000064E", Compile(layout).Layers[0].Keys[0].Label);
    }

    [TestMethod]
    public void DisplaysShouldLabelAKeyByItsIdOrItsText()
    {
        var layout = Layout(Layer("base", "* {sym}"), Layer("symbols", "1")) with { Displays = new() { ["*"] = "×", ["sym"] = "#+=" } };

        var layer = Compile(layout).Layers[0];

        Assert.AreEqual("×", layer.Keys[0].Label);
        Assert.AreEqual("*", layer.Keys[0].Output);
        Assert.AreEqual("#+=", layer.Keys[1].Label);
    }

    [TestMethod]
    public void MultiTapItemsShouldAlsoBeTheLongPressAlternatesOfAKeyWithNone()
    {
        var layout = Layout(Layer("base", "2")) with { Keys = new() { ["2"] = new() { MultiTap = ["a", "b", "c"] } } };

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, Compile(layout).Layers[0].Keys[0].LongPress.ToArray());
    }

    // ---------------------------------------------------------------- geometry

    [TestMethod]
    public void ANarrowerRowShouldBeCenteredAndAStretchingKeyShouldTakeTheRoomLeft()
    {
        var layout = Layout(Layer("base", "q w e r", "a s d", "{space}"));

        var compiled = Compile(layout);
        var layer = compiled.Layers[0];

        Assert.AreEqual(16, compiled.Columns);
        Assert.AreEqual(2, Key(layer, "a").Column, "half a key of room on each side");
        Assert.AreEqual(16, Key(layer, "space").ColumnSpan);
    }

    [TestMethod]
    public void TheRoomLeftShouldBeSharedBetweenStretchingKeysTheLastTakingTheRemainder()
    {
        var layout = Layout(Layer("base", "a b c d e f g h i j k*0.75", "x {space} {s2}")) with
        {
            Keys = new() { ["s2"] = new() { Output = "_", Stretch = true } },
        };

        var row = Compile(layout).Layers[0].Keys.Where(k => k.Row == 1).ToList();

        Assert.AreEqual(4, row[0].ColumnSpan);
        Assert.AreEqual(8 + 13, row[1].ColumnSpan); // 2 units + half of 27 quarters
        Assert.AreEqual(4 + 14, row[2].ColumnSpan);
    }

    [TestMethod]
    public void ATallKeyShouldHoldItsCellsInTheRowsBelowWithoutOverlap()
    {
        var layout = Layout(Layer("base", "1 2 3 {bksp}", "4 5 6 {enter}", "7 8 9", "0*2 {decimal}")) with
        {
            Id = "x-test",
            Keys = new() { ["enter"] = new() { Height = 3 } },
        };

        var compiled = Compile(layout);
        var layer = compiled.Layers[0];

        Assert.AreEqual(16, compiled.Columns);
        AssertNoOverlap(compiled);
        CollectionAssert.AreEqual(new[] { 16, 16, 16, 16 }, RowCoverage(layer));
        Assert.AreEqual(0, Key(layer, "7").Column, "a row sharing cells with a tall key starts at the start");
        Assert.AreEqual(12, Key(layer, "enter").Column);
        Assert.AreEqual(3, Key(layer, "enter").RowSpan);
    }

    [TestMethod]
    public void ATallStretchingKeyShouldTakeNoMoreRoomThanEveryRowItSpansHasLeft()
    {
        var layout = Layout(Layer("base", "a b {big}", "c d k l", "m n o p q r")) with
        {
            Keys = new() { ["big"] = new() { Output = "_", Stretch = true, Height = 2 } },
        };

        var compiled = Compile(layout);
        var layer = compiled.Layers[0];

        Assert.AreEqual(24, compiled.Columns);
        AssertNoOverlap(compiled);
        Assert.AreEqual(8, Key(layer, "big").ColumnSpan, "row 2 has one key of room left, not the three row 1 has");
        CollectionAssert.AreEqual(new[] { 16, 24, 24 }, RowCoverage(layer), "the room row 1 has left beyond that stays empty");
    }

    [TestMethod]
    public void TheGridShouldBeAsWideAsTheWidestRowOfAnyLayer()
    {
        var layout = Layout(Layer("base", "a b"), Layer("more", "1 2 3 4 5"));

        var compiled = Compile(layout);

        Assert.AreEqual(20, compiled.Columns);
        Assert.AreEqual(6, Key(compiled.Layers[0], "a").Column);
    }

    [TestMethod]
    public void RowAlignStartAndEndShouldPlaceANarrowRow()
    {
        var layout = Layout(new BitVirtualKeyboardLayer
        {
            Id = "base",
            Rows = ["a b c d", new BitVirtualKeyboardRow { Keys = [new() { Output = "s" }], Align = BitVirtualKeyboardRowAlign.Start },
                    new BitVirtualKeyboardRow { Keys = [new() { Output = "e" }], Align = BitVirtualKeyboardRowAlign.End }],
        });

        var layer = Compile(layout).Layers[0];

        Assert.AreEqual(0, Key(layer, "s").Column);
        Assert.AreEqual(12, Key(layer, "e").Column);
    }

    // ---------------------------------------------------------------- scramble

    [TestMethod]
    public void AScrambleShouldMoveTheDigitsBetweenSlotsAndLeaveTheOtherKeys()
    {
        var layout = Layout(Layer("base", "1 2 3", "4 5 6", "7 8 9", "{bksp} 0 {enter}")) with { Id = "x-test" };

        var plain = Compile(layout).Layers[0];
        var moved = Enumerable.Range(1, 20).Select(seed => Compile(layout, seed: seed).Layers[0]).ToList();

        Assert.IsTrue(moved.Any(l => Key(l, "1").Column != Key(plain, "1").Column || Key(l, "1").Row != Key(plain, "1").Row));

        foreach (var layer in moved)
        {
            Assert.AreEqual(Key(plain, "bksp").Column, Key(layer, "bksp").Column);
            Assert.AreEqual(Key(plain, "enter").Row, Key(layer, "enter").Row);
            CollectionAssert.AreEquivalent(plain.Keys.Select(k => (k.Row, k.Column)).ToList(), layer.Keys.Select(k => (k.Row, k.Column)).ToList());
            CollectionAssert.AreEqual(plain.Keys.Select(k => k.Output).ToList(), layer.Keys.Select(k => k.Output).ToList(), "keys keep their order and outputs");
        }
    }

    // ---------------------------------------------------------------- indices and cache

    [TestMethod]
    public void EveryKeyShouldHaveItsOwnIndexAcrossTheLayers()
    {
        var compiled = Compile(BitVirtualKeyboardLayouts.EnglishCompact);

        var indices = compiled.AllKeys.Select(k => k.Index).ToList();

        CollectionAssert.AreEqual(Enumerable.Range(0, indices.Count).ToList(), indices);
    }

    [TestMethod]
    public void TwoLayoutsOfTheSameContentShouldCompileOnce()
    {
        Assert.AreSame(Compile(BitVirtualKeyboardLayouts.English), Compile(BitVirtualKeyboardLayouts.English));
        Assert.AreNotSame(Compile(BitVirtualKeyboardLayouts.English), Compile(BitVirtualKeyboardLayouts.English, BitVirtualKeyboardPurpose.Email));
    }
}