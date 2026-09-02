using System;

namespace Bit.Butil;

/// <summary>
/// The modifier keys a <see cref="Keyboard"/> shortcut requires. Combine them with <c>|</c>; the
/// match is exact, so a shortcut registered for <see cref="Ctrl"/> alone does not fire when Shift
/// is also down.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/KeyboardEvent/getModifierState">KeyboardEvent.getModifierState()</see>
/// </summary>
[Flags]
public enum ButilModifiers
{
    /// <summary>No modifier - the key on its own.</summary>
    None = 0,

    /// <summary>Alt, which is Option on macOS.</summary>
    Alt = 1,

    /// <summary>Control.</summary>
    Ctrl = 2,

    /// <summary>The Meta key: Command on macOS, the Windows key elsewhere.</summary>
    Meta = 4,

    /// <summary>Shift.</summary>
    Shift = 8
}
