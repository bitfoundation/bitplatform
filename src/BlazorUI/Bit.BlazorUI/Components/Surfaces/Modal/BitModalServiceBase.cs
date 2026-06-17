using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The shared base for a service that shows any content inside a centralized modal using a modal container.
/// </summary>
/// <typeparam name="TReference">The concrete modal reference type returned by the Show methods.</typeparam>
/// <typeparam name="TParameters">The parameters type used to customize the shown modal.</typeparam>
/// <remarks>
/// LIFETIME: this service keeps a reference to the currently mounted modal container component and routes
/// every Show/Close through it. It is therefore tied to a single rendering scope. Register it as <c>Scoped</c>
/// for Blazor Server (one instance per circuit/user). Registering it as a <c>Singleton</c> is only safe for
/// single-user hosting models (Blazor WebAssembly and Hybrid/MAUI); a singleton on Blazor Server would be
/// shared across circuits, leaking modals between users and holding on to disposed containers.
/// <br/>
/// Calling a Show overload while no container is mounted only renders the modal later if it is
/// <c>persistent</c> (persistent modals are tracked and injected into the next container that mounts).
/// A non-persistent modal shown with no active container is not rendered and its reference is inert.
/// </remarks>
public abstract class BitModalServiceBase<TReference, TParameters>
    where TReference : BitModalReferenceBase<TReference, TParameters>
    where TParameters : class, new()
{
    private BitModalContainerBase<TReference, TParameters>? _container;
    // Persistent modals are tracked in a non-destructive list (not a drained queue) so they survive
    // container remounts: when the active container is disposed and a new one mounts, InitContainer
    // re-injects the still-open persistent modals into it. Entries are removed when their modal is
    // closed (see Close) so a closed persistent modal doesn't reappear after a remount.
    private readonly List<TReference> _persistentModals = [];
    private readonly object _persistentModalsLock = new();



    /// <summary>
    /// The event for when a new modal gets added through calling the Show method.
    /// </summary>
    public event Func<TReference, Task>? OnAddModal;

    /// <summary>
    /// The event for when a modal gets removed through calling the Close method.
    /// </summary>
    public event Func<TReference, Task>? OnCloseModal;



    /// <summary>
    /// Initializes the current modal container that is responsible for rendering the modals.
    /// </summary>
    /// <remarks>
    /// This may be called more than once over the application lifetime: when a container is disposed it
    /// calls <see cref="RemoveContainer"/> (clearing the reference), and a newly mounted container then
    /// re-initializes the service. The most recently initialized container becomes the active one and the
    /// still-open persistent modals are (re-)injected into it. Mounting multiple containers simultaneously
    /// is not supported; the last one to initialize wins.
    /// </remarks>
    public void InitContainer(BitModalContainerBase<TReference, TParameters> container)
    {
        _container = container;

        TReference[] persistentModals;
        lock (_persistentModalsLock)
        {
            persistentModals = [.. _persistentModals];
        }

        _container.InjectPersistentModals(persistentModals);
    }

    /// <summary>
    /// Detaches the given container if it is the one currently in use. Called when the container is disposed
    /// so the service doesn't keep a reference to (and try to render through) a torn-down container.
    /// </summary>
    public void RemoveContainer(BitModalContainerBase<TReference, TParameters> container)
    {
        if (ReferenceEquals(_container, container))
        {
            _container = null;
        }
    }

    /// <summary>
    /// Closes an already opened modal using its reference.
    /// </summary>
    public async Task Close(TReference modalRef)
    {
        // Mark the reference closed up front so any add handler still iterating in a concurrent Show
        // (a handler may close the modal mid-show) can detect the close and skip (re-)adding it.
        modalRef.MarkClosed();

        // Stop tracking persistent modals once closed so they aren't re-injected on a container remount.
        if (modalRef.Persistent)
        {
            lock (_persistentModalsLock)
            {
                _persistentModals.Remove(modalRef);
            }
        }

        var modalClose = OnCloseModal;
        if (modalClose is not null)
        {
            // Invoke every handler even if an earlier one throws, so a single failing handler can't
            // leave the modal half-removed (e.g. the container handler not running). Failures are
            // collected and rethrown together after all handlers have had a chance to run.
            List<Exception>? exceptions = null;
            foreach (var handler in modalClose.GetInvocationList().Cast<Func<TReference, Task>>())
            {
                try
                {
                    await handler(modalRef);
                }
                catch (Exception ex)
                {
                    (exceptions ??= []).Add(ex);
                }
            }

            if (exceptions is not null)
            {
                throw new AggregateException(exceptions);
            }
        }
    }

    /// <summary>
    /// Refreshes all open modals, invalidating their memoized merged parameters and re-rendering them.
    /// Call this after mutating modal parameters in place (which doesn't change object references).
    /// </summary>
    public Task Refresh()
    {
        return _container?.Refresh() ?? Task.CompletedTask;
    }

    /// <summary>
    /// Refreshes a specific open modal, invalidating its memoized merged parameters and re-rendering it.
    /// Call this after mutating the parameters of a single modal in place.
    /// </summary>
    public Task Refresh(TReference modalRef)
    {
        return _container?.Refresh(modalRef) ?? Task.CompletedTask;
    }

    /// <summary>
    /// Shows a new modal. When <paramref name="persistent"/> is true, the modal persists through the lifecycle of the application until it gets closed.
    /// </summary>
    public Task<TReference> Show<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        bool persistent = false) where T : IComponent
    {
        return Show<T>(null, null, persistent);
    }

    /// <summary>
    /// Shows a new modal with a custom component with parameters as its content.
    /// </summary>
    public Task<TReference> Show<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        Dictionary<string, object>? parameters, bool persistent = false) where T : IComponent
    {
        return Show<T>(parameters, null, persistent);
    }

    /// <summary>
    /// Shows a new modal with a custom component with parameters as its content.
    /// </summary>
    public Task<TReference> Show<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        Dictionary<string, object> parameters) where T : IComponent
    {
        return Show<T>(parameters, null, false);
    }

    /// <summary>
    /// Shows a new modal with a custom component as its content with custom parameters for the modal.
    /// </summary>
    public Task<TReference> Show<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        TParameters modalParameters) where T : IComponent
    {
        return Show<T>(null, modalParameters, false);
    }

    /// <summary>
    /// Shows a new modal with a custom component as its content with custom parameters for the modal.
    /// </summary>
    public Task<TReference> Show<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        TParameters? modalParameters, bool persistent = false) where T : IComponent
    {
        return Show<T>(null, modalParameters, persistent);
    }

    /// <summary>
    /// Shows a new modal with a custom component as its content with custom parameters for the custom component and the modal.
    /// </summary>
    public async Task<TReference> Show<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        Dictionary<string, object>? parameters,
        TParameters? modalParameters,
        bool persistent = false) where T : IComponent
    {
        var componentType = typeof(T);

        var modalReference = CreateReference(persistent);
        modalReference.SetParameters(modalParameters);

        var content = new RenderFragment(builder =>
        {
            var i = 0;
            builder.OpenComponent(i++, componentType);

            if (parameters is not null)
            {
                foreach (var parameter in parameters)
                {
                    builder.AddAttribute(i++, parameter.Key, parameter.Value);
                }
            }

            builder.AddComponentReferenceCapture(i, c => { modalReference.SetContent(c); });
            builder.CloseComponent();
        });

        var modal = BuildModalFragment(modalReference, content);
        modalReference.SetModal(modal);

        // Track every persistent modal (regardless of whether a container currently exists) so it can be
        // (re-)injected into the active container, including after a container remount. This must happen
        // before invoking the OnAddModal handlers: a handler may close the modal during its execution, and
        // Close can only remove the reference if it's already tracked here. Tracking after the handlers ran
        // would let such a close slip through, leaving a closed modal to reappear on a container remount.
        if (persistent)
        {
            lock (_persistentModalsLock)
            {
                _persistentModals.Add(modalReference);
            }
        }

        var modalAdd = OnAddModal;
        if (modalAdd is not null)
        {
            try
            {
                foreach (var handler in modalAdd.GetInvocationList().Cast<Func<TReference, Task>>())
                {
                    await handler(modalReference);

                    // A handler may have closed the modal during its execution (e.g. via Close on the
                    // reference). Stop here so a later handler can't re-add an already-closed modal back
                    // into a container.
                    if (modalReference.IsClosed) break;
                }
            }
            catch
            {
                // A handler threw before the modal was fully registered with a container. Undo the
                // persistent tracking added above so a failed Show doesn't leave a stale persistent
                // entry that would reappear on a container remount. (Remove is a no-op if an earlier
                // handler already closed and untracked the modal.)
                if (persistent)
                {
                    lock (_persistentModalsLock)
                    {
                        _persistentModals.Remove(modalReference);
                    }
                }

                // Earlier handlers may have already added (and rendered) the modal in a container.
                // Roll that state back by invoking the close handlers so a failed Show doesn't leave
                // a partially-added, visible modal behind. Removing an unknown modal is a no-op.
                var modalCloseRollback = OnCloseModal;
                if (modalCloseRollback is not null)
                {
                    foreach (var handler in modalCloseRollback.GetInvocationList().Cast<Func<TReference, Task>>())
                    {
                        // Swallow exceptions from individual rollback handlers so one failing handler
                        // doesn't (a) prevent the remaining handlers from rolling back their state, or
                        // (b) replace the original Show failure below. The root cause is preserved by
                        // the throw that follows once all handlers have run.
                        try
                        {
                            await handler(modalReference);
                        }
                        catch
                        {
                            // Intentionally ignored: continue rolling back and rethrow the original error.
                        }
                    }
                }

                throw;
            }
        }

        return modalReference;
    }



    /// <summary>
    /// Creates a new concrete modal reference bound to this service.
    /// </summary>
    protected abstract TReference CreateReference(bool persistent);

    /// <summary>
    /// Builds the render fragment that hosts the concrete modal component wrapping the given content.
    /// </summary>
    protected abstract RenderFragment BuildModalFragment(TReference modalReference, RenderFragment content);
}
