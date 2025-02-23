﻿using System.Collections.Concurrent;

namespace Bit.BlazorUI;

public partial class BitModalContainer : IDisposable
{
    private bool _disposed;
    private readonly List<BitModalReference> _modalRefs = [];



    [Parameter] public BitModalParameters ModalParameters { get; set; } = new();



    [Inject] private BitModalService _modalService { get; set; } = default!;



    internal void InjectPersistentModals(ConcurrentQueue<BitModalReference> queue)
    {
        while (queue.TryDequeue(out var modalRef))
        {
            _modalRefs.Add(modalRef);
        }
    }



    protected override void OnInitialized()
    {
        base.OnInitialized();

        _modalService.InitContainer(this);

        _modalService.OnAddModal += OnModalAdd;
        _modalService.OnCloseModal += OnCloseModal;
    }



    private Task OnModalAdd(BitModalReference modalRef)
    {
        if (_modalRefs.Contains(modalRef)) return Task.CompletedTask;

        _modalRefs.Add(modalRef);
        return InvokeAsync(StateHasChanged);
    }

    private Task OnCloseModal(BitModalReference modalRef)
    {
        _modalRefs.Remove(modalRef);
        return InvokeAsync(StateHasChanged);
    }



    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed || disposing is false) return;

        _modalService.OnAddModal -= OnModalAdd;
        _modalService.OnCloseModal -= OnCloseModal;

        _disposed = true;
    }
}
