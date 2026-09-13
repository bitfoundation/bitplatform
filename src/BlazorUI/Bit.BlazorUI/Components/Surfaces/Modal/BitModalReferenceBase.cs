namespace Bit.BlazorUI;

/// <summary>
/// The shared base for a reference to a modal instance that is shown using a modal service.
/// </summary>
/// <typeparam name="TReference">The concrete reference type (self-referencing for the closing call).</typeparam>
/// <typeparam name="TParameters">The parameters type used to customize the shown modal.</typeparam>
public abstract class BitModalReferenceBase<TReference, TParameters>
    where TReference : BitModalReferenceBase<TReference, TParameters>
    where TParameters : class, new()
{
    private readonly BitModalServiceBase<TReference, TParameters> _modalService;

    // Completed the first time the modal is closed, with whatever it was closed with. RunContinuationsAsynchronously
    // keeps whoever is awaiting the result off the stack that closed the modal, so a continuation of theirs can
    // never run in the middle of the close and re-enter the service.
    private readonly TaskCompletionSource<object?> _resultSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    // Completed with true the first time a container renders this modal, and with false if the modal is closed
    // before that ever happens - a modal shown while no container is mounted is never rendered, and whoever is
    // waiting for its content has to be let go rather than left waiting forever.
    private readonly TaskCompletionSource<bool> _renderSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    // Which call was the one that closed this modal, so that the answer does not depend on the result task
    // having been completed - the result is handed over after IsDismissed is settled, not before.
    private int _closed;



    public string Id { get; init; }

    public bool Persistent { get; }

    /// <summary>
    /// Indicates that this modal has been closed. Once closed a reference is never reused (each Show
    /// creates a new reference), so this flag stays set and lets in-flight add handlers detect a modal
    /// that was closed mid-show and avoid (re-)adding it.
    /// </summary>
    public bool IsClosed { get; private set; }

    /// <summary>
    /// Whether the modal was closed by the user rather than by the application: the close button, a click on the
    /// overlay, or the Escape key.
    /// </summary>
    /// <remarks>
    /// This is what tells a modal that was walked away from apart from one that was answered with nothing, which
    /// <see cref="Result"/> alone cannot: both complete with <c>null</c>. Check it where the two mean different
    /// things - a wizard step that treats "cancelled" as an answer of its own.
    /// </remarks>
    public bool IsDismissed { get; private set; }

    /// <summary>
    /// The instance of the component rendered as the content of the modal.
    /// </summary>
    /// <remarks>
    /// The instance is captured while the modal is rendered, which is after the Show call that created this
    /// reference returns, so this is still <c>null</c> immediately afterwards. Await <see cref="Rendered"/> - or
    /// use <see cref="GetContentAsync{T}"/>, which does it - to reach the content the moment there is one.
    /// <br/>
    /// It stays <c>null</c> for a modal shown with markup rather than with a component: markup is not a component
    /// instance, so there is none to hand back.
    /// </remarks>
    public object? Content { get; private set; }

    public RenderFragment? Modal { get; private set; }

    public TParameters? Parameters { get; private set; }

    /// <summary>
    /// Completes when the modal is closed, with the value it was closed with - <c>null</c> for a modal that was
    /// dismissed rather than answered, or closed through the parameterless <see cref="Close()"/>.
    /// </summary>
    /// <remarks>
    /// This is what turns a modal shown through the service into a question that can be awaited:
    /// <code>
    /// var modal = await modalService.Show&lt;ConfirmContent&gt;();
    /// var answer = await modal.Result;
    /// </code>
    /// The content of the modal answers with <see cref="CloseWith(object?)"/>.
    /// <br/>
    /// A modal that is never closed never completes, so only await this where the modal has a way out - which
    /// every modal shown to a user should have.
    /// </remarks>
    public Task<object?> Result => _resultSource.Task;

    /// <summary>
    /// Completes with <c>true</c> once a container has rendered this modal, and with <c>false</c> for a modal
    /// that was closed before it ever rendered.
    /// </summary>
    /// <remarks>
    /// A Show call hands the modal to the container, which renders it on its next render - after the call
    /// returns. Awaiting this is what turns "shown" into "on the screen", which is when <see cref="Content"/>
    /// holds the component instance and the content can be reached, measured or scripted.
    /// <br/>
    /// The <c>false</c> case is the modal that never made it: one shown while no container was mounted, or one
    /// closed in the same breath it was shown. It completes rather than hanging so that a caller waiting on the
    /// content of a modal that will never render is let go instead of left waiting forever.
    /// </remarks>
    public Task<bool> Rendered => _renderSource.Task;



    protected BitModalReferenceBase(BitModalServiceBase<TReference, TParameters> modalService, bool persistent)
    {
        Id = BitShortId.NewId();
        _modalService = modalService;
        Persistent = persistent;
    }



    internal void SetContent(object content)
    {
        Content = content;
    }

    internal void SetModal(RenderFragment modal)
    {
        Modal = modal;
    }

    internal void SetParameters(TParameters? parameters)
    {
        Parameters = parameters;
    }

    /// <summary>
    /// Reports that a container has rendered this modal. Only the first render counts: the task is what turns
    /// "shown" into "on the screen", and a modal is only put on the screen once.
    /// </summary>
    internal void MarkRendered()
    {
        _renderSource.TrySetResult(true);
    }

    /// <summary>
    /// Reports that this modal will never be rendered, which is what a modal shown while no container was
    /// mounted is: nothing tracks it, and a container mounting later takes on the persistent modals only.
    /// Whoever is awaiting <see cref="Rendered"/> - or the content behind it - is let go here rather than
    /// left waiting on a render that was never going to come.
    /// </summary>
    internal void MarkNeverRendered()
    {
        _renderSource.TrySetResult(false);
    }

    /// <summary>
    /// Marks this reference closed with the given result, returning whether this call is the one that closed it:
    /// false for a modal that was already closed, whose original result stands.
    /// </summary>
    internal bool MarkClosed(object? result, bool dismissed = false)
    {
        IsClosed = true;

        // A modal can be asked to close more than once (a close button and the overlay racing, a container
        // tearing down mid-close), and only the first answer is the answer.
        if (Interlocked.Exchange(ref _closed, 1) == 1) return false;

        // Settled before the result is handed over: completing the task is what schedules whoever is
        // awaiting it, and they read this to tell a dismissal from a close that carried a null result.
        IsDismissed = dismissed;

        _resultSource.TrySetResult(result);

        // A modal closed before it was ever rendered is one that will never be rendered, so whoever is waiting
        // on its content is let go here rather than left waiting on a render that is no longer coming.
        _renderSource.TrySetResult(false);

        return true;
    }

    /// <summary>
    /// Closes the modal without a result.
    /// </summary>
    /// <remarks>
    /// This is the application closing the modal, so a <c>CanClose</c> guard is not asked: use
    /// <see cref="TryClose"/> where the guard is to have a say.
    /// </remarks>
    public Task Close()
    {
        return _modalService.Close((TReference)this, null);
    }

    /// <summary>
    /// Closes the modal with the given result, which is what <see cref="Result"/> completes with.
    /// </summary>
    /// <remarks>
    /// A method of its own rather than an overload of <see cref="Close()"/>, so that <c>Close</c> stays a method
    /// group with a single signature: an overload set would make every existing
    /// <c>EventCallback.Factory.Create(this, modalRef.Close)</c> ambiguous, and the overload the compiler could
    /// pick for it takes the event argument as the result.
    /// </remarks>
    public Task CloseWith(object? result)
    {
        return _modalService.Close((TReference)this, result);
    }

    /// <summary>
    /// Asks the modal to close, and reports whether it did: a modal whose <c>CanClose</c> guard turns the close
    /// down stays open and this answers <c>false</c>.
    /// </summary>
    /// <remarks>
    /// This is the programmatic counterpart of the ways the user closes a modal, which are guarded the same way.
    /// A modal without a guard always closes, so this only answers <c>false</c> where a guard said so - or where
    /// the modal was already closed.
    /// </remarks>
    public Task<bool> TryClose(object? result = null)
    {
        return _modalService.TryClose((TReference)this, result);
    }

    /// <summary>
    /// Closes the modal as a dismissal - the way the close button, the overlay and the Escape key close it -
    /// which asks the <c>CanClose</c> guard first and marks the reference <see cref="IsDismissed"/>.
    /// </summary>
    /// <remarks>
    /// Use this for the content's own "cancel" action, so that a caller reading <see cref="IsDismissed"/> sees
    /// the same thing whether the user cancelled from inside the modal or walked away from it.
    /// </remarks>
    public Task<bool> Dismiss()
    {
        return _modalService.Dismiss((TReference)this, null);
    }

    // The dismissal the Modal itself reports, once it has already put its close guard to the user. The guard
    // it asked is the same one the service reads, so this close does not ask again: a guard that puts a
    // confirmation on the screen would otherwise put a second one there for the same dismissal.
    internal Task DismissFromModal()
    {
        return _modalService.DismissFromModal((TReference)this, null);
    }

    /// <summary>
    /// Replaces the parameters the modal is shown with and re-renders it.
    /// </summary>
    /// <remarks>
    /// The whole set is replaced rather than merged, so pass the full set the modal is to carry from now on. It
    /// is merged with the container-level parameters again on the next render, exactly as the original set was.
    /// <br/>
    /// Mutating the members of the existing parameters object works too - it is the same object the modal reads -
    /// but nothing notices such a change on its own, so follow it with a <c>Refresh</c>.
    /// </remarks>
    public Task Update(TParameters? parameters)
    {
        SetParameters(parameters);

        return _modalService.Refresh((TReference)this);
    }

    /// <summary>
    /// The result the modal was closed with, cast to <typeparamref name="T"/>, or <c>default</c> for a modal
    /// that was dismissed or answered with something else.
    /// </summary>
    /// <remarks>
    /// The typed counterpart of <see cref="Result"/>, for the common case of a modal that answers with a value
    /// of a known type: <c>var confirmed = await modal.GetResult&lt;bool&gt;();</c> answers <c>false</c> for a
    /// modal that was dismissed instead of throwing on the <c>null</c>.
    /// </remarks>
    public async Task<T?> GetResult<T>()
    {
        var result = await Result;

        return result is T value ? value : default;
    }

    /// <summary>
    /// The component rendered as the content of the modal, cast to <typeparamref name="T"/>, waiting for the
    /// modal to be rendered first. <c>default</c> for a modal that never rendered, or whose content is markup
    /// rather than a component of that type.
    /// </summary>
    /// <remarks>
    /// This is the way to reach the content right after showing a modal, since the content is only instantiated
    /// when the container renders it:
    /// <code>
    /// var modal = await modalService.Show&lt;EditorContent&gt;();
    /// var editor = await modal.GetContentAsync&lt;EditorContent&gt;();
    /// </code>
    /// </remarks>
    public async Task<T?> GetContentAsync<T>()
    {
        await Rendered;

        return Content is T content ? content : default;
    }
}
