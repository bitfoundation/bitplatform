namespace Bit.Bheadless;

/// <summary>What a thumb of a range <see cref="BitBhSlider"/> does when it meets its neighbour.</summary>
public enum BitBhSliderThumbCollision
{
    /// <summary>It stops at the neighbour (keeping <c>MinStepsBetweenThumbs</c> apart).</summary>
    Stop,

    /// <summary>It pushes the neighbour along (Base UI's default).</summary>
    Push,

    /// <summary>It passes the neighbour: the two swap places, and the keyboard focus follows the moving value.</summary>
    Swap,
}
