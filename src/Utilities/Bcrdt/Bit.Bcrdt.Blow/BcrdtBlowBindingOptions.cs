using Bit.Blow;

namespace Bit.Bcrdt.Blow;

/// <summary>Settings for a <see cref="BcrdtBlowBinding"/>.</summary>
public sealed class BcrdtBlowBindingOptions
{
    /// <summary>
    /// The origin the binding tags its transactions with. Null uses the binding itself.
    /// </summary>
    /// <remarks>
    /// The origin is how the binding tells its own writes from everybody else's, so they are never applied back to the
    /// graph they came from, and how an undo manager knows which transactions are this user's. Use
    /// <see cref="BcrdtBlowBinding.CreateUndoManager"/>, which tracks it for you.
    /// </remarks>
    public object? Origin { get; init; }

    /// <summary>
    /// How nodes and edges are written into the shared state and read back - the data mappings
    /// (<c>MapNodeData</c>, <c>MapEdgeData</c>) and the limits applied to what a participant sends. Null uses Blow's
    /// defaults.
    /// </summary>
    /// <remarks>
    /// Every participant should use the same mappings, as they would for files they exchange. Selection is never
    /// shared, whatever <see cref="BlowSerializerOptions.IncludeSelection"/> says: it is each participant's own, and
    /// travels through presence instead.
    /// </remarks>
    public BlowSerializerOptions? SerializerOptions { get; init; }

    /// <summary>
    /// Wraps the application of a change from the shared state to the lists, so the canvas can treat it as one edit
    /// from somewhere else.
    /// </summary>
    /// <remarks>
    /// <para>
    /// On a canvas, pass
    /// <c>edit =&gt; canvas.EditAsync(edit, "remote", BlowChangeOrigin.Remote, recordHistory: false)</c>: the canvas
    /// then renders once, raises <c>OnGraphChanged</c> with <see cref="BlowChangeOrigin.Remote"/> (which
    /// <see cref="BcrdtBlowBinding.OnGraphChangedAsync"/> ignores) and writes the change into its history rather than
    /// recording it, so its undo never takes back a collaborator's edit.
    /// </para>
    /// <para>
    /// The wrapper must run the edit before it returns, as <c>EditAsync</c> does: the binding applies a change while
    /// the shared document is reporting it. One that does not is run anyway, straight away, and the wrapper's own
    /// later call does nothing. Null applies changes to the lists directly; handle
    /// <see cref="BcrdtBlowBinding.RemoteChanged"/> to refresh whatever shows them.
    /// </para>
    /// </remarks>
    public Func<Action, Task>? RemoteEditScope { get; init; }

    /// <summary>
    /// The presence to publish the local cursor and selection through, and to read everyone else's from. Null turns
    /// presence off.
    /// </summary>
    public BcrdtPresenceService? Presence { get; init; }
}
