namespace Bit.Bmotion;

/// <summary>Named easing presets for tween transitions.</summary>
public enum BmEase
{
    Linear,
    In, Out, InOut,
    CircIn, CircOut, CircInOut,
    BackIn, BackOut, BackInOut,
    Anticipate,

    // ── Power curves ──────────────────────────────────────────────────────────
    SineIn, SineOut, SineInOut,
    QuadIn, QuadOut, QuadInOut,
    QuartIn, QuartOut, QuartInOut,
    QuintIn, QuintOut, QuintInOut,
    ExpoIn, ExpoOut, ExpoInOut,

    // ── Overshoot / oscillating (rAF path; sampled to linear() for the compositor) ──
    ElasticIn, ElasticOut, ElasticInOut,
    BounceIn, BounceOut, BounceInOut,
}
