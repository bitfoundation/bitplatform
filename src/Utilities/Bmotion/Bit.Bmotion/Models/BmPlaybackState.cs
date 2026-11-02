namespace Bit.Bmotion;

/// <summary>Where a programmatic animation is in its life (<see cref="BmAnimationControls.State"/>).</summary>
public enum BmPlaybackState
{
    /// <summary>Playing.</summary>
    Running,

    /// <summary>Held where it is (<see cref="BmAnimationControls.Pause"/>, a speed of 0, or a seek after it had ended).</summary>
    Paused,

    /// <summary>Over: it played to its end, or <see cref="BmAnimationControls.Complete"/> jumped it there.</summary>
    Finished,

    /// <summary>Stopped part-way (<see cref="BmAnimationControls.Stop"/>, or its cancellation token).</summary>
    Stopped,
}
