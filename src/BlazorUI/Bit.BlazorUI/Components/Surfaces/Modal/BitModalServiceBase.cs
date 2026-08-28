using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

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
/// A non-persistent modal shown with no active container is not rendered and its reference is inert - the
/// service logs that once, where a logger factory was handed to it.
/// </remarks>
public abstract class BitModalServiceBase<TReference, TParameters>
    where TReference : BitModalReferenceBase<TReference, TParameters>
    where TParameters : class, new()
{
    /// <summary>
    /// What is logged the first time a modal is shown with no container to render it, which is the one mistake
    /// that otherwise looks like nothing at all happening.
    /// </summary>
    internal const string MissingContainerMessage =
        "No modal container is mounted, so the modal was not rendered. Mount a single modal container " +
        "(for example <BitModalContainer />) in your layout, in the same interactive render mode as the " +
        "components that show modals.";

    private BitModalContainerBase<TReference, TParameters>? _container;
    // Persistent modals are tracked in a non-destructive list (not a drained queue) so they survive
    // container remounts: when the active container is disposed and a new one mounts, InitContainer
    // re-injects the still-open persistent modals into it. Entries are removed when their modal is
    // closed (see Close) so a closed persistent modal doesn't reappear after a remount.
    private readonly List<TReference> _persistentModals = [];
    private readonly object _persistentModalsLock = new();

    private readonly ILogger? _logger;
    // The missing container is reported once rather than once per modal: an app without a container shows
    // every one of its modals into nothing, and one clear line is the message - a line per modal is noise.
    private bool _missingContainerLogged;



    protected BitModalServiceBase(ILoggerFactory? loggerFactory = null)
    {
        _logger = loggerFactory?.CreateLogger(GetType());
    }



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
    /// is not supported; the last one to initialize wins, and the ones before it stop taking new modals so
    /// that a modal is never rendered twice.
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
    /// Whether the given container is the one this service currently renders through, which is the last one to
    /// have initialized. A container that is not the active one leaves the new modals to the one that is.
    /// </summary>
    internal bool IsActiveContainer(BitModalContainerBase<TReference, TParameters> container)
    {
        return ReferenceEquals(_container, container);
    }

    /// <summary>
    /// Whether a modal container is currently mounted for this service, i.e. whether a <c>Show</c> call right now
    /// would actually render its modal.
    /// </summary>
    /// <remarks>
    /// This reflects LIVE state, not registration: the service being registered in DI does not imply a container
    /// exists. A container attaches during its own initialization (after its first render) via <see cref="InitContainer"/>
    /// and detaches on dispose via <see cref="RemoveContainer"/>, so this can be <c>false</c> very early in a render
    /// cycle before any container has initialized, and it can flip back to <c>false</c> across a container remount.
    /// It is therefore reliable at the moment of a user-triggered <c>Show</c> (the layout has long since rendered),
    /// but should not be treated as a permanent guarantee.
    /// <br/>
    /// The main use is a caller that can show through more than one modal service (for example a wrapper that prefers
    /// one service but can fall back to another) and wants to pick a service whose container is actually mounted,
    /// because a non-persistent modal shown while this is <c>false</c> is silently not rendered (see the type remarks).
    /// </remarks>
    public bool IsContainerAvailable => _container is not null;

    /// <summary>
    /// The modals this service currently has open, in the order they were opened.
    /// </summary>
    /// <remarks>
    /// A snapshot rather than a live view, so iterating it while closing the modals in it is safe. It holds what
    /// the active container is rendering, plus the persistent modals that are still waiting for a container to
    /// mount - which are open too, only not on the screen yet.
    /// </remarks>
    public IReadOnlyList<TReference> OpenModals
    {
        get
        {
            List<TReference> openModals = _container is null ? [] : [.. _container.GetOpenModals()];

            lock (_persistentModalsLock)
            {
                foreach (var modalRef in _persistentModals)
                {
                    if (openModals.Contains(modalRef)) continue;

                    openModals.Add(modalRef);
                }
            }

            return openModals;
        }
    }

    /// <summary>
    /// The open modal with the given id, or <c>null</c> when there is none - the modal was closed, or the id
    /// belongs to another service.
    /// </summary>
    /// <remarks>
    /// The id is the one on the reference a Show call handed back. This is what lets code that only kept the id -
    /// a notification payload, a route parameter - reach the modal again.
    /// </remarks>
    public TReference? GetModal(string? id)
    {
        if (id is null) return null;

        foreach (var modalRef in OpenModals)
        {
            if (modalRef.Id == id) return modalRef;
        }

        return null;
    }

    /// <summary>
    /// Closes an already opened modal using its reference.
    /// </summary>
    /// <remarks>
    /// This is the application closing the modal, so a close guard on its parameters is not asked. Use
    /// <see cref="TryClose"/> where the guard is to have a say.
    /// </remarks>
    public Task Close(TReference modalRef)
    {
        return Close(modalRef, null);
    }

    /// <summary>
    /// Closes an already opened modal using its reference, with the result its
    /// <see cref="BitModalReferenceBase{TReference, TParameters}.Result"/> completes with.
    /// </summary>
    /// <remarks>
    /// This is the application closing the modal, so a close guard on its parameters is not asked. Use
    /// <see cref="TryClose"/> where the guard is to have a say.
    /// </remarks>
    public Task Close(TReference modalRef, object? result)
    {
        return Close(modalRef, result, false);
    }

    private async Task Close(TReference modalRef, object? result, bool dismissed)
    {
        ArgumentNullException.ThrowIfNull(modalRef);

        // Mark the reference closed up front so any add handler still iterating in a concurrent Show
        // (a handler may close the modal mid-show) can detect the close and skip (re-)adding it.
        //
        // A modal can be asked to close more than once (a close button and the overlay racing, a container tearing
        // down mid-close). Only the first close is the close: it keeps the result, and it is the only one the close
        // handlers are run for, so a modal is never removed - or reported closed - twice.
        if (modalRef.MarkClosed(result, dismissed) is false) return;

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
    /// Asks a modal to close, and reports whether it did: a modal whose close guard turns the close down stays
    /// open and this answers <c>false</c>.
    /// </summary>
    /// <remarks>
    /// The guard is what a modal with something to lose - a half-filled form, an upload still running - answers
    /// with. A modal that declares none always closes, so this only answers <c>false</c> where a guard said so,
    /// or where the modal had already been closed.
    /// </remarks>
    public Task<bool> TryClose(TReference modalRef, object? result = null)
    {
        return TryClose(modalRef, result, false);
    }

    /// <summary>
    /// Closes a modal as a dismissal - the way the close button, the overlay and the Escape key close it - which
    /// asks the close guard first and marks the reference as dismissed.
    /// </summary>
    internal Task<bool> Dismiss(TReference modalRef, object? result)
    {
        return TryClose(modalRef, result, true);
    }

    private async Task<bool> TryClose(TReference modalRef, object? result, bool dismissed)
    {
        ArgumentNullException.ThrowIfNull(modalRef);

        if (modalRef.IsClosed) return false;

        var canClose = GetCloseGuard(modalRef);
        if (canClose is not null && await canClose() is false)
        {
            // The modal may already have taken itself off the screen before asking - the close button and the
            // Escape key both close the modal first and report it afterwards - so a refusal has to put it back.
            // Re-rendering the container hands the modal its "open" parameter again, which is what brings it back.
            if (dismissed && _container is not null)
            {
                await _container.Refresh(modalRef);
            }

            return false;
        }

        // Another close may have landed while the guard was being awaited, in which case that one is the close.
        if (modalRef.IsClosed) return false;

        await Close(modalRef, result, dismissed);

        return true;
    }

    /// <summary>
    /// The guard to ask before a modal is closed by the user, or <c>null</c> for a modal that declares none.
    /// The base type has no opinion on what a set of parameters looks like, so a concrete service reads it from
    /// the parameters its modals are shown with - through <see cref="GetEffectiveParameters"/>, so that a
    /// container-level guard is honored as the default for every modal it renders.
    /// </summary>
    protected virtual Func<Task<bool>>? GetCloseGuard(TReference modalRef) => null;

    /// <summary>
    /// The parameters the given modal effectively carries: its own merged with the container-level ones, or -
    /// with no container mounted to merge them - the ones it was shown with.
    /// </summary>
    protected TParameters? GetEffectiveParameters(TReference modalRef)
    {
        return _container is null ? modalRef.Parameters : _container.GetEffectiveParameters(modalRef);
    }

    /// <summary>
    /// Closes every modal this service currently has open, each with a <c>null</c> result.
    /// </summary>
    /// <remarks>
    /// The modals are closed in the order they were opened, and the set to close is taken before any of them
    /// is closed, so a modal opened by a close handler of an earlier one is left alone rather than being
    /// closed by the same call that opened it. Modals shown while no container is mounted are not tracked
    /// here (persistent ones excepted), so they are not reached by this either.
    /// <br/>
    /// This is the application closing the modals, so the close guards are not asked: a sign-out or a
    /// navigation is not something a half-filled form gets to turn down.
    /// </remarks>
    public async Task CloseAll()
    {
        foreach (var modalRef in OpenModals)
        {
            if (modalRef.IsClosed) continue;

            await Close(modalRef);
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
        return Show<T>((Dictionary<string, object>?)null, null, persistent);
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
        return Show<T>((Dictionary<string, object>?)null, modalParameters, false);
    }

    /// <summary>
    /// Shows a new modal with a custom component as its content with custom parameters for the modal.
    /// </summary>
    public Task<TReference> Show<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        TParameters? modalParameters, bool persistent = false) where T : IComponent
    {
        return Show<T>((Dictionary<string, object>?)null, modalParameters, persistent);
    }

    /// <summary>
    /// Shows a new modal with a custom component as its content with custom parameters for the custom component and the modal.
    /// </summary>
    public Task<TReference> Show<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        Dictionary<string, object>? parameters,
        TParameters? modalParameters,
        bool persistent = false) where T : IComponent
    {
        return Show(typeof(T), parameters, modalParameters, persistent);
    }

    /// <summary>
    /// Shows a new modal with a component whose type is only known at run time as its content.
    /// </summary>
    /// <remarks>
    /// The counterpart of the <c>Show&lt;T&gt;</c> overloads for the callers that pick the content from a map, a
    /// registry or a route rather than by naming it in code. The type has to be a Blazor component.
    /// </remarks>
    /// <exception cref="ArgumentException">The type is not a Blazor component.</exception>
    public Task<TReference> Show(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType,
        Dictionary<string, object>? parameters = null,
        TParameters? modalParameters = null,
        bool persistent = false)
    {
        return Show(componentType, _ => parameters, modalParameters, persistent);
    }

    /// <summary>
    /// Shows a new modal with the given markup as its content, for the content that is not worth a component
    /// of its own - a line of text, a confirmation, a fragment the caller already has in hand.
    /// </summary>
    /// <remarks>
    /// The reference's <see cref="BitModalReferenceBase{TReference, TParameters}.Content"/> stays <c>null</c>
    /// for a modal shown this way: markup is not a component instance, so there is none to hand back. Use one
    /// of the <c>Show&lt;T&gt;</c> overloads where the content has to be reached after it is shown.
    /// </remarks>
    public Task<TReference> Show(RenderFragment content, TParameters? modalParameters = null, bool persistent = false)
    {
        ArgumentNullException.ThrowIfNull(content);

        var modalReference = CreateReference(persistent);
        modalReference.SetParameters(modalParameters);

        return Show(modalReference, content, persistent);
    }

    /// <summary>
    /// Shows a new modal, building the content component's parameters from a factory that receives the modal reference.
    /// Use this overload when a parameter needs the reference itself (e.g. an <c>OnClose</c> callback that closes this
    /// very modal): the reference is handed to the factory before the content is rendered, so the callback can never
    /// observe an unassigned reference the way it can when the reference is only captured after Show returns.
    /// </summary>
    public Task<TReference> Show<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        Func<TReference, Dictionary<string, object>?> parametersFactory,
        TParameters? modalParameters = null,
        bool persistent = false) where T : IComponent
    {
        return Show(typeof(T), parametersFactory, modalParameters, persistent);
    }

    /// <summary>
    /// Shows a new modal with a component whose type is only known at run time, building its parameters from a
    /// factory that receives the modal reference.
    /// </summary>
    /// <exception cref="ArgumentException">The type is not a Blazor component.</exception>
    public async Task<TReference> Show(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType,
        Func<TReference, Dictionary<string, object>?> parametersFactory,
        TParameters? modalParameters = null,
        bool persistent = false)
    {
        ArgumentNullException.ThrowIfNull(componentType);
        ArgumentNullException.ThrowIfNull(parametersFactory);

        // Checked here rather than left to the renderer: the type is what the caller got wrong, and the
        // exception the renderer throws several layers down names the render tree instead.
        if (typeof(IComponent).IsAssignableFrom(componentType) is false)
        {
            throw new ArgumentException($"The type '{componentType.FullName}' is not a Blazor component.", nameof(componentType));
        }

        var modalReference = CreateReference(persistent);
        modalReference.SetParameters(modalParameters);

        // Build the content parameters with the reference already in hand so a parameter such as an OnClose
        // callback can capture modalReference.Close directly, closing the window a caller would otherwise face
        // when wiring the close callback only after Show returns.
        var parameters = parametersFactory(modalReference);

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

        return await Show(modalReference, content, persistent);
    }

    /// <summary>
    /// The shared tail of every Show overload: wraps the content in the concrete modal component, tracks the
    /// reference when it is persistent, and hands it to the add handlers - rolling all of that back if one
    /// of them throws.
    /// </summary>
    private async Task<TReference> Show(TReference modalReference, RenderFragment content, bool persistent)
    {
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

        // A non-persistent modal shown with nothing to render it is the one mistake that looks exactly like
        // nothing happening, so it is reported rather than swallowed. A persistent one is fine: it waits for
        // the next container to mount.
        if (_container is null && persistent is false)
        {
            LogMissingContainer();
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

                // The modal never made it onto the screen, so it is marked closed before the rollback runs:
                // that is what lets go of anyone waiting on its Result or on it being rendered, and it is
                // what the close handlers below see when they ask.
                modalReference.MarkClosed(null);

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

    private void LogMissingContainer()
    {
        if (_missingContainerLogged) return;

        _missingContainerLogged = true;

        _logger?.LogError(MissingContainerMessage);
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
