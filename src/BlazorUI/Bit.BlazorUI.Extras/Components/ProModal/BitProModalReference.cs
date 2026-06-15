namespace Bit.BlazorUI;

/// <summary>
/// A reference to the <see cref="BitProModal"/> instance that is shown using the <see cref="BitProModalService"/>.
/// </summary>
public class BitProModalReference
{
    private readonly BitProModalService _modalService;



    public string Id { get; init; }

    public bool Persistent { get; private set; }

    public object? Content { get; private set; }

    public RenderFragment? Modal { get; private set; }

    public BitProModalParameters? Parameters { get; private set; }



    public BitProModalReference(BitProModalService modalService, bool persistent)
    {
        Id = BitShortId.NewId();
        _modalService = modalService;
        Persistent = persistent;
    }



    public void SetContent(object content)
    {
        Content = content;
    }

    public void SetModal(RenderFragment modal)
    {
        Modal = modal;
    }

    public void SetParameters(BitProModalParameters? parameters)
    {
        Parameters = parameters;
    }

    public void Close()
    {
        _ = _modalService.Close(this);
    }
}
