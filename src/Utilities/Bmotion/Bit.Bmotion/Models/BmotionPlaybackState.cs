namespace Bit.Bmotion;

/// <summary>Where a programmatic animation is in its life (<see cref="BmotionAnimationControls.State"/>).</summary>
public enum BmotionPlaybackState
{
    /// <summary>Playing.</summary>
    Running,

    /// <summary>Held where it is (<see cref="BmotionAnimationControls.Pause"/>, a speed of 0, or a seek after it had ended).</summary>
    Paused,

    /// <summary>Over: it played to its end, or <see cref="BmotionAnimationControls.Complete"/> jumped it there.</summary>
    Finished,

    /// <summary>Stopped part-way (<see cref="BmotionAnimationControls.Stop"/>, or its cancellation token).</summary>
    Stopped,
}
