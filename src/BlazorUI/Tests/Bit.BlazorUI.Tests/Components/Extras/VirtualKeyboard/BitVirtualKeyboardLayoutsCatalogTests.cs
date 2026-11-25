using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Bit.BlazorUI.Tests.Components.Extras.VirtualKeyboard.BitVirtualKeyboardTestLayouts;

namespace Bit.BlazorUI.Tests.Components.Extras.VirtualKeyboard;

[TestClass]
public class BitVirtualKeyboardLayoutsCatalogTests
{
    [TestMethod]
    public void EveryAccessShouldBuildAFreshLayout()
    {
        Assert.AreNotSame(BitVirtualKeyboardLayouts.English, BitVirtualKeyboardLayouts.English);
        Assert.AreNotSame(BitVirtualKeyboardLayouts.English.Keys, BitVirtualKeyboardLayouts.English.Keys);
    }

    [TestMethod]
    public void EveryBuiltInLayoutShouldCompileForEveryPurposeAndMode()
    {
        foreach (var id in BitVirtualKeyboardLayouts.Ids)
        {
            var layout = BitVirtualKeyboardLayouts.Get(id);
            Assert.IsNotNull(layout, id);
            Assert.AreEqual(id, layout.Id);

            foreach (var purpose in new[] { BitVirtualKeyboardPurpose.Text, BitVirtualKeyboardPurpose.Email, BitVirtualKeyboardPurpose.Url, BitVirtualKeyboardPurpose.Search })
            {
                foreach (var mode in new[] { BitVirtualKeyboardMode.Inline, BitVirtualKeyboardMode.Docked })
                {
                    foreach (var languages in new[] { 1, 2 })
                    {
                        var compiled = Compile(layout, purpose, mode, languages);
                        AssertNoOverlap(compiled);
                    }
                }
            }
        }
    }

    [TestMethod]
    public void EveryRowOfEveryFullLayerShouldBeFifteenUnitsWide()
    {
        foreach (var id in BitVirtualKeyboardLayouts.Ids.Where(id => id.EndsWith("-compact") is false && id.StartsWith("x-") is false))
        {
            foreach (var mode in new[] { BitVirtualKeyboardMode.Inline, BitVirtualKeyboardMode.Docked })
            {
                var compiled = Compile(BitVirtualKeyboardLayouts.Get(id)!, mode: mode, languages: 2);

                Assert.AreEqual(60, compiled.Columns, id);
                foreach (var layer in compiled.Layers)
                {
                    CollectionAssert.AreEqual(Enumerable.Repeat(60, layer.Rows).ToArray(), RowCoverage(layer), $"{id} {layer.Id}");
                }
            }
        }
    }

    [TestMethod]
    public void EveryRowOfACompactLayoutShouldFillItsGridOrBeACenteredLetterRow()
    {
        var compiled = Compile(BitVirtualKeyboardLayouts.EnglishCompact, BitVirtualKeyboardPurpose.Email, languages: 2);

        Assert.AreEqual(40, compiled.Columns);
        foreach (var layerId in new[] { "symbols", "symbols2" })
        {
            CollectionAssert.AreEqual(new[] { 40, 40, 40, 40 }, RowCoverage(compiled.GetLayer(layerId)!), layerId);
        }
        CollectionAssert.AreEqual(new[] { 40, 36, 40, 40 }, RowCoverage(compiled.GetLayer("base")!));
        Assert.AreEqual(2, Key(compiled.GetLayer("base")!, "a").Column, "the A row is indented half a key");
    }

    [TestMethod]
    public void TheEnglishFullLayoutShouldHoldTheUsRows()
    {
        var compiled = Compile(BitVirtualKeyboardLayouts.English);
        var baseLayer = compiled.GetLayer("base")!;
        var shift = compiled.GetLayer("shift")!;

        string Row(BitVirtualKeyboardCompiledLayer layer, int row) => string.Join(" ", layer.Keys.Where(k => k.Row == row && k.Action == BitVirtualKeyboardKeyAction.Character).Select(k => k.Output));

        Assert.AreEqual("` 1 2 3 4 5 6 7 8 9 0 - =", Row(baseLayer, 0));
        Assert.AreEqual(@"q w e r t y u i o p [ ] \", Row(baseLayer, 1));
        Assert.AreEqual("a s d f g h j k l ; '", Row(baseLayer, 2));
        Assert.AreEqual("z x c v b n m , . /", Row(baseLayer, 3));
        Assert.AreEqual("~ ! @ # $ % ^ & * ( ) _ +", Row(shift, 0));
        Assert.AreEqual("Q W E R T Y U I O P { } |", Row(shift, 1));
        Assert.AreEqual("A S D F G H J K L : \"", Row(shift, 2));
        Assert.AreEqual("Z X C V B N M < > ?", Row(shift, 3));
    }

    [TestMethod]
    public void TheEnglishCompactLayoutShouldHoldTheLettersAndOfferTheCapitalsOfItsAlternates()
    {
        var compiled = Compile(BitVirtualKeyboardLayouts.EnglishCompact);

        Assert.AreEqual("e", Key(compiled.GetLayer("base")!, "e").Output);
        CollectionAssert.AreEqual(new[] { "è", "é", "ê", "ë", "ē" }, Key(compiled.GetLayer("base")!, "e").LongPress.ToArray());
        CollectionAssert.AreEqual(new[] { "È", "É", "Ê", "Ë", "Ē" }, Key(compiled.GetLayer("shift")!, "E").LongPress.ToArray());
        Assert.AreEqual("?123", Key(compiled.GetLayer("base")!, "sym").Label);
        Assert.AreEqual(@"=\<", Key(compiled.GetLayer("symbols")!, "layer:symbols2").Label);
    }

    [TestMethod]
    public void GetShouldFindALayoutByItsIdInAnyCase()
    {
        Assert.AreEqual("en", BitVirtualKeyboardLayouts.Get("EN")!.Id);
        Assert.AreEqual("en-compact", BitVirtualKeyboardLayouts.Get("en-Compact")!.Id);
        Assert.IsNull(BitVirtualKeyboardLayouts.Get("xx"));
    }
}