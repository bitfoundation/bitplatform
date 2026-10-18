namespace Bit.Balette.Tests;

[TestClass]
public class KeyboardShortcutTests
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
        Assert.AreEqual(expected, BaletteKeyboardShortcut.Parse(input).Normalized);
    }

    [TestMethod]
    public void Rejects_chord_with_two_keys()
    {
        Assert.ThrowsExactly<FormatException>(() => BaletteKeyboardShortcut.Parse("a+b"));
    }

    [TestMethod]
    public void Rejects_modifier_only_chord()
    {
        Assert.ThrowsExactly<FormatException>(() => BaletteKeyboardShortcut.Parse("ctrl+shift"));
    }

    [TestMethod]
    public void TryParse_returns_false_on_garbage()
    {
        Assert.IsFalse(BaletteKeyboardShortcut.TryParse("ctrl+", out _));
        Assert.IsFalse(BaletteKeyboardShortcut.TryParse("", out _));
        Assert.IsTrue(BaletteKeyboardShortcut.TryParse("mod+k", out var s));
        Assert.AreEqual("mod+k", s!.Normalized);
    }

    [TestMethod]
    public void Display_tokens_are_platform_aware()
    {
        var s = BaletteKeyboardShortcut.Parse("Mod+Shift+K");
        AssertSequence.AreEqual(new[] { "⌘", "⇧", "K" }, s.DisplayTokens(isMac: true)[0]);
        AssertSequence.AreEqual(new[] { "Ctrl", "Shift", "K" }, s.DisplayTokens(isMac: false)[0]);
    }

    [TestMethod]
    public void Space_is_one_chord_and_displays_as_Space()
    {
        var s = BaletteKeyboardShortcut.Parse("Ctrl+Space");
        Assert.IsFalse(s.IsSequence);
        Assert.DoesNotContain(" ", s.Normalized);
        Assert.AreEqual("Ctrl+Space", s.ToDisplayString(isMac: false));
    }

    [TestMethod]
    public void Sequences_display_with_then()
    {
        var s = BaletteKeyboardShortcut.Parse("G I");
        Assert.IsTrue(s.IsSequence);
        Assert.AreEqual("G then I", s.ToDisplayString(isMac: false));
    }

    [TestMethod]
    public void Equality_is_by_normalized_form()
    {
        Assert.AreEqual(BaletteKeyboardShortcut.Parse("Shift+Ctrl+P"), BaletteKeyboardShortcut.Parse("ctrl+shift+p"));
        Assert.AreEqual(BaletteKeyboardShortcut.Parse("mod+k").GetHashCode(), BaletteKeyboardShortcut.Parse("Mod+K").GetHashCode());
    }

    [TestMethod]
    public void Implicit_conversion_from_string()
    {
        BaletteKeyboardShortcut s = "Ctrl+B";
        Assert.AreEqual("ctrl+b", s.Normalized);
    }
}
