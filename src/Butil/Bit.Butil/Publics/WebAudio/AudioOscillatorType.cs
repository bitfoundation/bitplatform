namespace Bit.Butil;

/// <summary>
/// Mirrors <see href="https://developer.mozilla.org/en-US/docs/Web/API/OscillatorNode/type">OscillatorNode.type</see>.
/// </summary>
/// <remarks>
/// All four are band-limited, so none of them alias however high the frequency goes.
/// </remarks>
public enum AudioOscillatorType
{
    /// <summary>A pure tone with no harmonics. What a test tone or a soft beep wants.</summary>
    Sine,

    /// <summary>Odd harmonics only - hollow, and loud for its amplitude. The classic chiptune lead.</summary>
    Square,

    /// <summary>Every harmonic - the brightest of the four, and the usual starting point for synthesised strings and brass.</summary>
    Sawtooth,

    /// <summary>Odd harmonics that fall away quickly - softer than a square, less pure than a sine.</summary>
    Triangle
}
