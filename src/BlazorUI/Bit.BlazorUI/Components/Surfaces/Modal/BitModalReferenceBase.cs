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

    internal void MarkClosed()
    {
        IsClosed = true;
    }

    public Task Close()
    {
        return _modalService.Close((TReference)this);
    }
}
