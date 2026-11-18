namespace Bit.Bmotion;

/// <summary>What happened to (part of) an animation.</summary>
public enum BmotionAnimationEventKind
{
    /// <summary>It started playing with the event's <see cref="BmotionAnimationEvent.Strategy"/>.</summary>
    Started,

    /// <summary>It reached its end.</summary>
    Finished,

    /// <summary>
    /// It ended before its end: stopped, replaced by a newer animation of the same properties, its element removed,
    /// or the start overtaken before it played.
    /// </summary>
    Interrupted,
}
