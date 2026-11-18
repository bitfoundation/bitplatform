using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>Reading direction. Flips horizontal arrow-key navigation and start/end alignment.</summary>
public enum BitBhDirection
{
    /// <summary>Left-to-right.</summary>
    Ltr,

    /// <summary>Right-to-left.</summary>
    Rtl,
}
