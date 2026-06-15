using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The shared base for a service that shows any content inside a centralized modal using a modal container.
/// </summary>
/// <typeparam name="TReference">The concrete modal reference type returned by the Show methods.</typeparam>
/// <typeparam name="TParameters">The parameters type used to customize the shown modal.</typeparam>
public abstract class BitModalServiceBase<TReference, TParameters>
    where TReference : BitModalReferenceBase<TReference, TParameters>
    where TParameters : class, new()
{
    private BitModalContainerBase<TReference, TParameters>? _container;
    private readonly ConcurrentQueue<TReference> _persistentModalsQueue = new();



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
    /// re-initializes the service. The most recently initialized container becomes the active one and any
    /// queued persistent modals are injected into it. Mounting multiple containers simultaneously is not
    /// supported; the last one to initialize wins.
    /// </remarks>
    public void InitContainer(BitModalContainerBase<TReference, TParameters> container)
    {
        _container = container;
        _container.InjectPersistentModals(_persistentModalsQueue);
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
        var modalClose = OnCloseModal;
        if (modalClose is not null)
        {
            foreach (var handler in modalClose.GetInvocationList().Cast<Func<TReference, Task>>())
            {
                await handler(modalRef);
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

        var modalAdd = OnAddModal;
        if (modalAdd is not null)
        {
            foreach (var handler in modalAdd.GetInvocationList().Cast<Func<TReference, Task>>())
            {
                await handler(modalReference);
            }
        }

        if (persistent && _container is null)
        {
            _persistentModalsQueue.Enqueue(modalReference);
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
