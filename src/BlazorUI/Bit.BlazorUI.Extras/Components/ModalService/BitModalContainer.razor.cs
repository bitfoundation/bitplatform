namespace Bit.BlazorUI;

public partial class BitModalContainer : IDisposable
{
    [Parameter] public BitModalParameters ModalParameters { get; set; } = new();



    [Inject] private BitModalService _modalService { get; set; } = default!;



    private readonly List<BitModalReference> _modals = [];



    protected override void OnInitialized()
    {
        base.OnInitialized();

        _modalService.OnAddModal += OnModalAdd;
        _modalService.OnCloseModal += OnCloseModal;
    }



    private Task OnModalAdd(BitModalReference modal)
    {
        _modals.Add(modal);
        return InvokeAsync(StateHasChanged);
    }

    private Task OnCloseModal(BitModalReference modal)
    {
        _modals.Remove(modal);
        return InvokeAsync(StateHasChanged);
    }



    public void Dispose()
    {
        _modalService.OnAddModal -= OnModalAdd;
        _modalService.OnCloseModal -= OnCloseModal;
    }
}
