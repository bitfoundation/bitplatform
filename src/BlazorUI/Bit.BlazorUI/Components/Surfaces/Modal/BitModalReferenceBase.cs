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

    public Task Close()
    {
        return _modalService.Close((TReference)this);
    }
}
