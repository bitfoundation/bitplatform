namespace Bit.BlazorUI;

/// <summary>
/// A reference to the <see cref="BitModal"/> instance that is shown using the <see cref="BitModalService"/>.
/// </summary>
public class BitModalReference
{
    private readonly BitModalService _modalService;



    public string Id { get; init; }

    public bool Persistent { get; private set; }

    public object? Content { get; private set; }

    public RenderFragment? Modal { get; private set; }

    public BitModalParameters? Parameters { get; private set; }



    public BitModalReference(BitModalService modalService, bool persistent)
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

    internal void SetParameters(BitModalParameters? parameters)
    {
        Parameters = parameters;
    }

    public void Close()
    {
        _ = _modalService.Close(this);
    }
}
