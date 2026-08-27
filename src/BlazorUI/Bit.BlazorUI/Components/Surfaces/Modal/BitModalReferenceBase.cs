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



    public string Id { get; init; }

    public bool Persistent { get; private set; }

    /// <summary>
    /// Indicates that this modal has been closed. Once closed a reference is never reused (each Show
    /// creates a new reference), so this flag stays set and lets in-flight add handlers detect a modal
    /// that was closed mid-show and avoid (re-)adding it.
    /// </summary>
    public bool IsClosed { get; private set; }

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
    /// Marks this reference closed with the given result, returning whether this call is the one that closed it:
    /// false for a modal that was already closed, whose original result stands.
    /// </summary>
    internal bool MarkClosed(object? result)
    {
        IsClosed = true;

        // TrySet rather than Set: a modal can be asked to close more than once (a close button and the
        // overlay racing, a container tearing down mid-close), and only the first answer is the answer.
        return _resultSource.TrySetResult(result);
    }

    /// <summary>
    /// Closes the modal without a result.
    /// </summary>
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
}
