﻿namespace Bit.BlazorUI;

public partial class BitModalContainer
{
    private readonly List<BitModalReference> _modals = [];



    [Inject] private BitModalService _modalService { get; set; } = default!;



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

    private void OnCloseModal(BitModalReference modal)
    {
        _modals.Remove(modal);
        StateHasChanged();
    }
}
