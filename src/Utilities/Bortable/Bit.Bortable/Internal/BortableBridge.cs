using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop;

namespace Bit.Bortable;

/// <summary>
/// The object <c>bit-bortable.js</c> calls a list through. The browser layer routes the input - it finds the list
/// under the pointer that takes the drag and hit-tests it - and calls in only when something changes: a drag
/// starts, the drop position moves, a key is pressed on an item, the drag ends. Kept apart from the component so
/// the <c>[JSInvokable]</c> plumbing never shows in its public API.
/// </summary>
internal sealed class BortableBridge(IBortableZone zone)
{
    /// <summary>A drag of the item rendered with <paramref name="key"/> starts, made with <paramref name="input"/>.</summary>
    [JSInvokable]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BortableStartResult))]
    public Task<BortableStartResult?> Start(string key, string input) => zone.StartAsync(key, input);

    /// <summary>
    /// The pointer reached drop position <paramref name="index"/> of this list. Answers 1 when a list changed (the
    /// browser layer plays the slide), 0 when nothing did, -1 when the list refused the position (it shows the
    /// pointer as unable to drop there).
    /// </summary>
    /// <remarks>Every call about a drag names it by the id <c>Start</c> answered (<paramref name="drag"/>): a late call
    /// about a drag that has since ended is ignored.</remarks>
    [JSInvokable]
    public Task<int> Over(int index, int drag) => zone.OverAsync(index, drag);

    /// <summary>
    /// The drag ended; <paramref name="how"/> is <c>drop</c>, <c>release</c>, <c>cancel</c> or <c>abandon</c>. Answers
    /// what to announce.
    /// </summary>
    [JSInvokable]
    public Task<string?> End(string how, int drag) => zone.EndAsync(how, drag);

    /// <summary>
    /// A keyboard action on the item rendered with <paramref name="key"/>: <c>pick</c>, <c>select</c>, <c>to</c> (the
    /// item moves to <paramref name="index"/> of the list <paramref name="list"/>, this one when <c>null</c>),
    /// <c>drop</c> or <c>cancel</c>; <paramref name="drag"/> names the keyboard drag the step belongs to.
    /// </summary>
    [JSInvokable]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BortableKeyResult))]
    public Task<BortableKeyResult> Key(string key, string action, string? list, int index, int drag) => zone.KeyAsync(key, action, list, index, drag);

    /// <summary>The item rendered with <paramref name="key"/> got the focus: it becomes the list's tab stop.</summary>
    [JSInvokable]
    public void Focus(string key) => zone.Focus(key);
}
