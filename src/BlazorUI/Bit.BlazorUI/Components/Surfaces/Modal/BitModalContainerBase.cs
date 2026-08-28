using System.Diagnostics;
using Microsoft.AspNetCore.Components.Routing;

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

    // The path the app was on when this container last looked. A modal belongs to the page it was opened from,
    // so a change of this - and only of this, not of a query string or a fragment - closes the modals that
    // didn't ask to outlive it.
    private string? _currentPath;



    // Resolved through the provider rather than injected, for the same reason the snack bar does it: a
    // container has to keep working in a host that has no router at all - a MAUI window, a test bed - where
    // injecting a NavigationManager would take the whole container down over a feature it never uses.
    [Inject] private IServiceProvider _serviceProvider { get; set; } = default!;

    private NavigationManager? _navigationManager;



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
    /// Whether the given modal asked to be closed when the app navigates somewhere else, or <c>null</c> where it
    /// said nothing - in which case it is closed, which is what a modal belonging to the page it was opened from
    /// wants. The base type has no opinion on what a set of parameters looks like, so a concrete container reads
    /// this from the parameters its modals are shown with.
    /// </summary>
    protected virtual bool? GetCloseOnNavigation(TReference modalRef) => null;



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
        if (_disposed) return Task.CompletedTask;

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
        if (_disposed) return Task.CompletedTask;

        return InvokeAsync(() =>
        {
            _mergedParametersCache.Remove(modalRef);
            StateHasChanged();
        });
    }



    /// <summary>
    /// The modals this container currently renders, in the order they were opened.
    /// </summary>
    internal IReadOnlyList<TReference> GetOpenModals()
    {
        return _modalRefs;
    }

    /// <summary>
    /// The parameters the given modal effectively carries: its own merged with the container-level ones. This
    /// is what the service reads for the options it acts on itself, so that a container-level default reaches
    /// them the same way it reaches the ones the modal renders with.
    /// </summary>
    internal TParameters? GetEffectiveParameters(TReference modalRef)
    {
        return GetMergedParameters(modalRef);
    }

    internal void InjectPersistentModals(IReadOnlyList<TReference> modals)
    {
        foreach (var modalRef in modals)
        {
            if (modalRef.IsClosed) continue;

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

        _navigationManager = _serviceProvider?.GetService(typeof(NavigationManager)) as NavigationManager;

        if (_navigationManager is not null)
        {
            _currentPath = GetPath(_navigationManager.Uri);
            _navigationManager.LocationChanged += OnLocationChanged;
        }
    }

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);

        // Everything in the list is on the screen by the time this runs, which is what a caller waiting to
        // reach the content of a modal it has just shown is waiting for. Reporting it more than once costs
        // nothing: only the first render of a modal is the one that completes its task.
        foreach (var modalRef in _modalRefs)
        {
            modalRef.MarkRendered();
        }
    }



    private Task OnModalAdd(TReference modalRef)
    {
        // Only the container the service is currently rendering through takes new modals. Mounting more than
        // one container is not supported, and without this every one of them would render the same modal.
        if (_disposed || ModalService.IsActiveContainer(this) is false) return Task.CompletedTask;

        return InvokeAsync(() =>
        {
            if (_modalRefs.Contains(modalRef)) return;

            _modalRefs.Add(modalRef);
            StateHasChanged();
        });
    }

    private Task OnCloseModal(TReference modalRef)
    {
        if (_disposed) return Task.CompletedTask;

        return InvokeAsync(() =>
        {
            _modalRefs.Remove(modalRef);
            _mergedParametersCache.Remove(modalRef);
            StateHasChanged();
        });
    }

    // A modal belongs to the page it was opened from: leaving that page leaves the modal saying nothing about
    // the page it is now lying over, so it is closed with the rest of what is being left behind. Only a change
    // of path counts - a query string or a fragment changed on the same page is still the same page - and a
    // modal that asked to outlive the route change is left alone.
    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        if (_disposed) return;

        var newPath = GetPath(e.Location);

        if (string.Equals(_currentPath, newPath, StringComparison.OrdinalIgnoreCase)) return;

        _currentPath = newPath;

        foreach (var modalRef in _modalRefs.ToArray())
        {
            if (modalRef.IsClosed) continue;

            if (GetCloseOnNavigation(modalRef) is false) continue;

            CloseInBackground(modalRef);
        }
    }

    // Closing is asynchronous and both callers - the navigation handler and the teardown - are not, so the
    // task is awaited here rather than dropped: an unobserved failure of a consumer's close handler would
    // otherwise resurface much later, on the finalizer thread, with nothing left to say where it came from.
    private async void CloseInBackground(TReference modalRef)
    {
        try
        {
            await ModalService.Close(modalRef);
        }
        catch (ObjectDisposedException) { } // the scope went away with the modal; nothing left to close
        catch (Exception ex)
        {
            Debug.WriteLine($"A close handler threw while closing a modal the container left behind: {ex}");
        }
    }

    private string? GetPath(string uri)
    {
        return _navigationManager?.ToAbsoluteUri(uri).AbsolutePath.TrimEnd('/');
    }



    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed || disposing is false) return;

        _disposed = true;

        ModalService.OnAddModal -= OnModalAdd;
        ModalService.OnCloseModal -= OnCloseModal;
        ModalService.RemoveContainer(this);

        if (_navigationManager is not null)
        {
            _navigationManager.LocationChanged -= OnLocationChanged;
            _navigationManager = null;
        }

        // The modals this container was rendering are off the screen the moment it is gone. A persistent one
        // is meant to survive that and is re-injected into the next container that mounts, but every other one
        // is over - and left unclosed it would leave whoever is awaiting its Result waiting forever. The
        // handlers are already detached above, so closing them here doesn't reach back into this container.
        foreach (var modalRef in _modalRefs.ToArray())
        {
            if (modalRef.Persistent || modalRef.IsClosed) continue;

            CloseInBackground(modalRef);
        }

        _modalRefs.Clear();
        _mergedParametersCache.Clear();
    }
}
