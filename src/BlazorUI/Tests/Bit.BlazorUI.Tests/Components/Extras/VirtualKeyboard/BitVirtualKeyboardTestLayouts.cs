using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.VirtualKeyboard;

// Helpers the layout tests share.
internal static class BitVirtualKeyboardTestLayouts
{
    public static BitVirtualKeyboardCompiledLayout Compile(BitVirtualKeyboardLayout layout,
                                                           BitVirtualKeyboardPurpose purpose = BitVirtualKeyboardPurpose.Text,
                                                           BitVirtualKeyboardMode mode = BitVirtualKeyboardMode.Inline,
                                                           int languages = 1,
                                                           string culture = "en-US",
                                                           bool autoShift = true,
                                                           BitVirtualKeyboardDigits digits = BitVirtualKeyboardDigits.Latin,
                                                           int? seed = null,
                                                           bool numberTarget = false)
    {
        return BitVirtualKeyboardLayoutCompiler.Compile(layout, new()
        {
            Purpose = purpose,
            Mode = mode,
            LanguagesCount = languages,
            NextIndicator = languages > 1 ? "FA" : null,
            Culture = CultureInfo.GetCultureInfo(culture),
            AutoShift = autoShift,
            Digits = digits,
            ScrambleSeed = seed,
            NumberTarget = numberTarget,
        });
    }

    public static BitVirtualKeyboardLayout Layout(params BitVirtualKeyboardLayer[] layers) => new()
    {
        Id = "test",
        Name = "Test",
        Layers = [.. layers],
    };

    public static BitVirtualKeyboardLayer Layer(string id, params string[] rows) => new()
    {
        Id = id,
        Rows = [.. rows.Select(r => (BitVirtualKeyboardRow)r)],
    };

    // How many quarter units of every row of a layer its keys cover (a tall key counting in every row it spans).
    public static int[] RowCoverage(BitVirtualKeyboardCompiledLayer layer)
    {
        var coverage = new int[layer.Rows];
        foreach (var key in layer.Keys)
        {
            for (var r = key.Row; r < key.Row + key.RowSpan; r++) coverage[r] += key.ColumnSpan;
        }
        return coverage;
    }

    public static void AssertNoOverlap(BitVirtualKeyboardCompiledLayout layout)
    {
        foreach (var layer in layout.Layers)
        {
            var cells = new HashSet<(int, int)>();
            foreach (var key in layer.Keys)
            {
                for (var r = key.Row; r < key.Row + key.RowSpan; r++)
                {
                    for (var c = key.Column; c < key.Column + key.ColumnSpan; c++)
                    {
                        Assert.IsTrue(cells.Add((r, c)), $"The key \"{key.KeyId}\" of the layer \"{layer.Id}\" overlaps another at row {r}, column {c}.");
                        Assert.IsTrue(c < layout.Columns, $"The key \"{key.KeyId}\" runs past the grid.");
                    }
                }
            }
        }
    }

    public static BitVirtualKeyboardCompiledKey Key(BitVirtualKeyboardCompiledLayer layer, string keyId) => layer.Keys.Single(k => k.KeyId == keyId);
}