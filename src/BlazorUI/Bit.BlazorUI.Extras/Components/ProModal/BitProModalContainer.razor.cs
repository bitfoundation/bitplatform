using System.Collections.Concurrent;

namespace Bit.BlazorUI;

public partial class BitProModalContainer : IDisposable
{
    private bool _disposed;
    private readonly List<BitProModalReference> _modalRefs = [];

    private BitProModalParameters? _lastModalParameters;
    private readonly Dictionary<BitProModalReference, BitProModalParameters?> _mergedParametersCache = [];



    [Parameter] public BitProModalParameters ModalParameters { get; set; } = new();



    /// <summary>
    /// Returns the merged parameters for the given modal reference, caching the result so that the
    /// cascading value keeps a stable identity across renders and doesn't force the whole
    /// <see cref="BitProModal"/> subtree to re-render. The cache is invalidated when the
    /// container's <see cref="ModalParameters"/> reference changes.
    /// </summary>
    private BitProModalParameters? GetMergedParameters(BitProModalReference modalRef)
    {
        if (!ReferenceEquals(_lastModalParameters, ModalParameters))
        {
            _lastModalParameters = ModalParameters;
            _mergedParametersCache.Clear();
        }

        if (_mergedParametersCache.TryGetValue(modalRef, out var merged)) return merged;

        merged = BitProModalParameters.Merge(modalRef.Parameters, ModalParameters);
        _mergedParametersCache[modalRef] = merged;
        return merged;
    }

    /// <summary>
    /// Invalidates the memoized merged parameters for all open modals and re-renders them.
    /// Call this after mutating <see cref="ModalParameters"/> (or a modal reference's parameters) in place,
    /// since such mutations don't change the object reference and therefore aren't detected automatically.
    /// </summary>
    public Task Refresh()
    {
        _mergedParametersCache.Clear();
        return InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Invalidates the memoized merged parameters for a specific modal reference and re-renders the modals.
    /// Call this after mutating the parameters of a single modal in place.
    /// </summary>
    public Task Refresh(BitProModalReference modalRef)
    {
        _mergedParametersCache.Remove(modalRef);
        return InvokeAsync(StateHasChanged);
    }



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
        _mergedParametersCache.Remove(modalRef);
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
