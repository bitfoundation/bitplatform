using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Bit.BlazorUI.Tests.Components.Extras.CommandPalette;

[TestClass]
public class BitCommandPaletteKeyboardShortcutTests
{
    [TestMethod]
    [DataRow("Mod+K", "mod+k")]
    [DataRow("ctrl+shift+p", "ctrl+shift+p")]
    [DataRow("Shift+Ctrl+P", "ctrl+shift+p")]
    [DataRow("Cmd+Enter", "meta+enter")]
    [DataRow("G I", "g i")]
    [DataRow("g  then", "g then")]
    [DataRow("Ctrl++", "ctrl++")]
    [DataRow("Alt+Up", "alt+arrowup")]
    [DataRow("Esc", "escape")]
    [DataRow("Ctrl+Space", "ctrl+space")]
    [DataRow("Mod+Spacebar", "mod+space")]
    [DataRow("Space G", "space g")]
    public void Parses_and_normalizes(string input, string expected)
    {
        Assert.AreEqual(expected, BitCommandPaletteKeyboardShortcut.Parse(input).Normalized);
    }

    [TestMethod]
    public void Rejects_chord_with_two_keys()
    {
        Assert.ThrowsExactly<FormatException>(() => BitCommandPaletteKeyboardShortcut.Parse("a+b"));
    }

    [TestMethod]
    public void Rejects_modifier_only_chord()
    {
        Assert.ThrowsExactly<FormatException>(() => BitCommandPaletteKeyboardShortcut.Parse("ctrl+shift"));
    }

    [TestMethod]
    public void TryParse_returns_false_on_garbage()
    {
        Assert.IsFalse(BitCommandPaletteKeyboardShortcut.TryParse("ctrl+", out _));
        Assert.IsFalse(BitCommandPaletteKeyboardShortcut.TryParse("", out _));
        Assert.IsTrue(BitCommandPaletteKeyboardShortcut.TryParse("mod+k", out var s));
        Assert.AreEqual("mod+k", s!.Normalized);
    }

    [TestMethod]
    public void Display_tokens_are_platform_aware()
    {
        var s = BitCommandPaletteKeyboardShortcut.Parse("Mod+Shift+K");
        AssertSequence.AreEqual(new[] { "⌘", "⇧", "K" }, s.DisplayTokens(isMac: true)[0]);
        AssertSequence.AreEqual(new[] { "Ctrl", "Shift", "K" }, s.DisplayTokens(isMac: false)[0]);
    }

    [TestMethod]
    public void Space_is_one_chord_and_displays_as_Space()
    {
        var s = BitCommandPaletteKeyboardShortcut.Parse("Ctrl+Space");
        Assert.IsFalse(s.IsSequence);
        Assert.DoesNotContain(" ", s.Normalized);
        Assert.AreEqual("Ctrl+Space", s.ToDisplayString(isMac: false));
    }

    [TestMethod]
    public void Sequences_display_with_then()
    {
        var s = BitCommandPaletteKeyboardShortcut.Parse("G I");
        Assert.IsTrue(s.IsSequence);
        Assert.AreEqual("G then I", s.ToDisplayString(isMac: false));
    }

    [TestMethod]
    public void Equality_is_by_normalized_form()
    {
        Assert.AreEqual(BitCommandPaletteKeyboardShortcut.Parse("Shift+Ctrl+P"), BitCommandPaletteKeyboardShortcut.Parse("ctrl+shift+p"));
        Assert.AreEqual(BitCommandPaletteKeyboardShortcut.Parse("mod+k").GetHashCode(), BitCommandPaletteKeyboardShortcut.Parse("Mod+K").GetHashCode());
    }

    [TestMethod]
    public void Implicit_conversion_from_string()
    {
        BitCommandPaletteKeyboardShortcut s = "Ctrl+B";
        Assert.AreEqual("ctrl+b", s.Normalized);
    }
}
