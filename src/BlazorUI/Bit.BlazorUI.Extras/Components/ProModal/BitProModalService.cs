using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// A service to show any content inside a centralized <see cref="BitProModal"/> using <see cref="BitProModalContainer"/>.
/// </summary>
public class BitProModalService
{
    private BitProModalContainer? _container;
    private readonly ConcurrentQueue<BitProModalReference> _persistentModalsQueue = new();



    /// <summary>
    /// The event for when a new modal gets added through calling the Show method.
    /// </summary>
    public event Func<BitProModalReference, Task>? OnAddModal;

    /// <summary>
    /// The event for when a modal gets removed through calling the Close method.
    /// </summary>
    public event Func<BitProModalReference, Task>? OnCloseModal;



    /// <summary>
    /// Initializes the current modal container that is responsible for rendering the modals.
    /// </summary>
    public void InitContainer(BitProModalContainer container)
    {
        _container = container;
        _container.InjectPersistentModals(_persistentModalsQueue);
    }

    /// <summary>
    /// Closes an already opened modal using its reference.
    /// </summary>
    public async Task Close(BitProModalReference modalRef)
    {
        var modalClose = OnCloseModal;
        if (modalClose is not null)
        {
            await modalClose(modalRef);
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
    public Task Refresh(BitProModalReference modalRef)
    {
        return _container?.Refresh(modalRef) ?? Task.CompletedTask;
    }

    /// <summary>
    /// Shows a new BitProModal. When <paramref name="persistent"/> is true, the modal persists through the lifecycle of the application until it gets closed.
    /// </summary>
    public Task<BitProModalReference> Show<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        bool persistent = false) where T : IComponent
    {
        return Show<T>(null, null, persistent);
    }

    /// <summary>
    /// Shows a new BitProModal with a custom component with parameters as its content.
    /// </summary>
    public Task<BitProModalReference> Show<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        Dictionary<string, object>? parameters, bool persistent = false) where T : IComponent
    {
        return Show<T>(parameters, null, persistent);
    }

    /// <summary>
    /// Shows a new BitProModal with a custom component with parameters as its content.
    /// </summary>
    public Task<BitProModalReference> Show<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        Dictionary<string, object> parameters) where T : IComponent
    {
        return Show<T>(parameters, null, false);
    }

    /// <summary>
    /// Shows a new BitProModal with a custom component as its content with custom parameters for the modal.
    /// </summary>
    public Task<BitProModalReference> Show<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        BitProModalParameters modalParameters) where T : IComponent
    {
        return Show<T>(null, modalParameters, false);
    }

    /// <summary>
    /// Shows a new BitProModal with a custom component as its content with custom parameters for the modal.
    /// </summary>
    public Task<BitProModalReference> Show<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        BitProModalParameters? modalParameters, bool persistent = false) where T : IComponent
    {
        return Show<T>(null, modalParameters, persistent);
    }

    /// <summary>
    /// Shows a new BitProModal with a custom component as its content with custom parameters for the custom component and the modal.
    /// </summary>
    public async Task<BitProModalReference> Show<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        Dictionary<string, object>? parameters,
        BitProModalParameters? modalParameters,
        bool persistent = false) where T : IComponent
    {
        var componentType = typeof(T);

        var modalReference = new BitProModalReference(this, persistent);
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

            builder.AddComponentReferenceCapture(i, c => { modalReference.SetContent((T)c); });
            builder.CloseComponent();
        });

        var modal = new RenderFragment(builder =>
        {
            var seq = 0;
            builder.OpenComponent<BitProModal>(seq++);
            builder.SetKey(modalReference.Id);
            builder.AddComponentParameter(seq++, nameof(BitProModal.IsOpen), true);
            builder.AddComponentParameter(seq++, nameof(BitProModal.IsOpenChanged), EventCallback.Factory.Create<bool>(modalReference, () => modalReference.Close()));
            builder.AddComponentParameter(seq++, nameof(BitProModal.ChildContent), content);
            builder.CloseComponent();
        });
        modalReference.SetModal(modal);

        var modalAdd = OnAddModal;
        if (modalAdd is not null)
        {
            await modalAdd(modalReference);
        }

        if (persistent && _container is null)
        {
            _persistentModalsQueue.Enqueue(modalReference);
        }

        return modalReference;
    }
}
