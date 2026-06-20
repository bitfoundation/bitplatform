namespace Bit.BlazorUI;

/// <summary>
/// The shared base for a container component that renders the modals shown through a modal service.
/// </summary>
/// <typeparam name="TReference">The concrete modal reference type managed by the matching service.</typeparam>
/// <typeparam name="TParameters">The parameters type used to customize the rendered modals.</typeparam>
public abstract class BitModalContainerBase<TReference, TParameters> : ComponentBase, IDisposable
    where TReference : BitModalReferenceBase<TReference, TParameters>
    where TParameters : class, new()
{
    private bool _disposed;
    protected readonly List<TReference> _modalRefs = [];

    private TParameters? _lastModalParameters;
    private readonly Dictionary<TReference, TParameters?> _mergedParametersCache = [];



    [Parameter] public TParameters ModalParameters { get; set; } = new();



    /// <summary>
    /// The modal service this container renders modals for.
    /// </summary>
    protected abstract BitModalServiceBase<TReference, TParameters> ModalService { get; }

    /// <summary>
    /// Merges the per-modal parameters (taking precedence) with the container-level <see cref="ModalParameters"/>.
    /// </summary>
    protected abstract TParameters? MergeParameters(TParameters? modalParameters, TParameters? containerParameters);



    /// <summary>
    /// Returns the merged parameters for the given modal reference, caching the result so that the
    /// cascading value keeps a stable identity across renders and doesn't force the whole modal
    /// subtree to re-render. The cache is invalidated when the container's <see cref="ModalParameters"/>
    /// reference changes.
    /// </summary>
    protected TParameters? GetMergedParameters(TReference modalRef)
    {
        if (!ReferenceEquals(_lastModalParameters, ModalParameters))
        {
            _lastModalParameters = ModalParameters;
            _mergedParametersCache.Clear();
        }

        if (_mergedParametersCache.TryGetValue(modalRef, out var merged)) return merged;

        merged = MergeParameters(modalRef.Parameters, ModalParameters);
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
        return InvokeAsync(() =>
        {
            _mergedParametersCache.Clear();
            StateHasChanged();
        });
    }

    /// <summary>
    /// Invalidates the memoized merged parameters for a specific modal reference and re-renders the modals.
    /// Call this after mutating the parameters of a single modal in place.
    /// </summary>
    public Task Refresh(TReference modalRef)
    {
        return InvokeAsync(() =>
        {
            _mergedParametersCache.Remove(modalRef);
            StateHasChanged();
        });
    }



    internal void InjectPersistentModals(IReadOnlyList<TReference> modals)
    {
        foreach (var modalRef in modals)
        {
            if (_modalRefs.Contains(modalRef)) continue;

            _modalRefs.Add(modalRef);
        }
    }



    protected override void OnInitialized()
    {
        base.OnInitialized();

        ModalService.InitContainer(this);

        ModalService.OnAddModal += OnModalAdd;
        ModalService.OnCloseModal += OnCloseModal;
    }



    private Task OnModalAdd(TReference modalRef)
    {
        return InvokeAsync(() =>
        {
            if (_modalRefs.Contains(modalRef)) return;

            _modalRefs.Add(modalRef);
            StateHasChanged();
        });
    }

    private Task OnCloseModal(TReference modalRef)
    {
        return InvokeAsync(() =>
        {
            _modalRefs.Remove(modalRef);
            _mergedParametersCache.Remove(modalRef);
            StateHasChanged();
        });
    }



    public void Dispose()
    {
        if (_disposed) return;

        ModalService.OnAddModal -= OnModalAdd;
        ModalService.OnCloseModal -= OnCloseModal;
        ModalService.RemoveContainer(this);

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
