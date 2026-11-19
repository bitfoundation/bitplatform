namespace Bit.Blow;

/// <summary>What the runner does with a node no handler is registered for.</summary>
public enum BlowMissingHandlerBehavior
{
    /// <summary>The run is refused before anything starts, with a <see cref="BlowRunCodes.MissingHandler"/> problem per node.</summary>
    Refuse,

    /// <summary>The node succeeds without doing anything, passing its first input value on (or nothing, when it has none).</summary>
    PassThrough,

    /// <summary>The node is skipped, and so is everything that only it reached.</summary>
    Skip,
}
