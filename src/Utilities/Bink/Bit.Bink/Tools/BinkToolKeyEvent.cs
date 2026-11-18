namespace Bit.Bink;

/// <summary>A keyboard event forwarded to tools.</summary>
/// <param name="Key">The <c>KeyboardEvent.key</c> value, e.g. <c>"Escape"</c> or <c>"a"</c>.</param>
/// <param name="Ctrl">Control (or Command on macOS) held.</param>
/// <param name="Shift">Shift held.</param>
/// <param name="Alt">Alt/Option held.</param>
public readonly record struct BinkToolKeyEvent(string Key, bool Ctrl, bool Shift, bool Alt);
