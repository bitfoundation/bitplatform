namespace Bit.BbScanner;

/// <summary>
/// What the device does, beyond the outline drawn around the symbol and the screen reader
/// announcement, each time a scan is reported. Combine the flags for both.
/// </summary>
/// <remarks>
/// Both are best effort, because browsers guard them: a beep only sounds once the user has
/// interacted with the page - a scanner that starts on its own may stay silent until the first
/// tap or click - and Safari, on iOS as on macOS, does not vibrate at all. Neither is ever the
/// only signal that a scan happened.
/// </remarks>
[Flags]
public enum BbScanFeedback
{
    /// <summary>No feedback beyond the outline and the announcement.</summary>
    None = 0,

    /// <summary>A short vibration, where the device and browser offer one.</summary>
    Vibrate = 1 << 0,

    /// <summary>A short tone, played through the Web Audio API; no audio file is loaded.</summary>
    Beep = 1 << 1,
}
