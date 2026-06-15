using System.Collections.Concurrent;

namespace Bit.BlazorUI;

public partial class BitProModalContainer : IDisposable
{
    private bool _disposed;
    private readonly List<BitProModalReference> _modalRefs = [];



    [Parameter] public BitProModalParameters ModalParameters { get; set; } = new();



    [Inject] private BitProModalService _modalService { get; set; } = default!;



    internal void InjectPersistentModals(ConcurrentQueue<BitProModalReference> queue)
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



    private Task OnModalAdd(BitProModalReference modalRef)
    {
        if (_modalRefs.Contains(modalRef)) return Task.CompletedTask;

        _modalRefs.Add(modalRef);
        return InvokeAsync(StateHasChanged);
    }

    private Task OnCloseModal(BitProModalReference modalRef)
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
