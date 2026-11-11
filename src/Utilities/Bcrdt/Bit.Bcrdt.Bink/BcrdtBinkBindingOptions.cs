using Bit.Bink;

namespace Bit.Bcrdt.Bink;

/// <summary>Settings for a <see cref="BcrdtBinkBinding"/>.</summary>
public sealed class BcrdtBinkBindingOptions
{
    /// <summary>
    /// The origin the binding tags its transactions with. Null uses the binding itself.
    /// </summary>
    /// <remarks>
    /// The origin is how the binding tells its own writes from everybody else's, so they are never applied back to the
    /// drawing they came from, and how an undo manager knows which transactions are this user's. Use
    /// <see cref="BcrdtBinkBinding.CreateUndoManager"/>, which tracks it for you.
    /// </remarks>
    public object? Origin { get; init; }

    /// <summary>
    /// The drawing's history - <c>BinkCanvas.History</c> on a canvas - whose steps mark where one gesture ends.
    /// </summary>
    /// <remarks>
    /// A drag changes the drawing on every frame, and each of those changes written as it happens would be its own
    /// transaction, its own update on the wire and its own step for a collaborative undo. With the history, changes
    /// are held until the history records a step, so one gesture is one transaction. Without it, every change the
    /// drawing reports is written at once.
    /// </remarks>
    public BinkHistoryManager? History { get; init; }

    /// <summary>How elements are written into the shared state. Null uses Bink's defaults.</summary>
    /// <remarks>Every participant should use the same options, as they would for files they exchange.</remarks>
    public BinkSerializerOptions? SerializerOptions { get; init; }

    /// <summary>
    /// How elements are read back, including the limits applied to what a participant sends. Null uses Bink's
    /// defaults.
    /// </summary>
    public BinkDeserializerOptions? DeserializerOptions { get; init; }

    /// <summary>
    /// The presence to publish the local cursor and selection through, and to read everyone else's from. Null turns
    /// presence off.
    /// </summary>
    public BcrdtPresenceService? Presence { get; init; }

    /// <summary>
    /// A selection to publish whenever it changes - <c>BinkCanvas.Selection</c> on a canvas. Needs
    /// <see cref="Presence"/>.
    /// </summary>
    public BinkSelection? Selection { get; init; }
}
