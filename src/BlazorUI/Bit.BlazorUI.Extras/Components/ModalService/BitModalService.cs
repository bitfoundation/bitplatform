using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// A core service to show any content inside a centralized <see cref="BitModal"/> using <see cref="BitModalContainer"/>.
/// </summary>
public class BitModalService
{
    private BitModalContainer? _container;
    private readonly ConcurrentQueue<BitModalReference> _persistentModalsQueue = new();



    /// <summary>
    /// The event for when a new modal gets added through calling the Show method.
    /// </summary>
    public event Func<BitModalReference, Task>? OnAddModal;

    /// <summary>
    /// The event for when a modal gets removed through calling the Close method.
    /// </summary>
    public event Func<BitModalReference, Task>? OnCloseModal;



    /// <summary>
    /// Initializes the current modal container that is responsible for rendering the modals.
    /// </summary>
    public void InitContainer(BitModalContainer container)
    {
        _container = container;
        _container.InjectPersistentModals(_persistentModalsQueue);
    }

    /// <summary>
    /// Closes an already opened modal using its reference.
    /// </summary>
    public async Task Close(BitModalReference modalRef)
    {
        var modalClose = OnCloseModal;
        if (modalClose is not null)
        {
            await modalClose(modalRef);
        }
    }

    /// <summary>
    /// Shows a new persistent BitModal that will persist through the lifecycle of the application until it gets shown.
    /// </summary>
    public Task<BitModalReference> Show<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        bool persistent = false)
    {
        return Show<T>(null, null, persistent);
    }

    /// <summary>
    /// Shows a new BitModal with a custom component with parameters as its content.
    /// </summary>
    public Task<BitModalReference> Show<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        Dictionary<string, object>? parameters, bool persistent = false)
    {
        return Show<T>(parameters, null, persistent);
    }

    /// <summary>
    /// Shows a new BitModal with a custom component with parameters as its content.
    /// </summary>
    public Task<BitModalReference> Show<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        Dictionary<string, object> parameters)
    {
        return Show<T>(parameters, null, false);
    }

    /// <summary>
    /// Shows a new BitModal with a custom component as its content with custom parameters for the modal.
    /// </summary>
    public Task<BitModalReference> Show<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        BitModalParameters modalParameters)
    {
        return Show<T>(null, modalParameters, false);
    }

    /// <summary>
    /// Shows a new BitModal with a custom component as its content with custom parameters for the modal.
    /// </summary>
    public Task<BitModalReference> Show<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        BitModalParameters? modalParameters, bool persistent = false)
    {
        return Show<T>(null, modalParameters, persistent);
    }

    /// <summary>
    /// Shows a new BitModal with a custom component as its content with custom parameters for the custom component and the modal.
    /// </summary>
    public async Task<BitModalReference> Show<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        Dictionary<string, object>? parameters,
        BitModalParameters? modalParameters,
        bool persistent = false)
    {
        var componentType = typeof(T);

        if (typeof(IComponent).IsAssignableFrom(componentType) is false)
        {
            throw new ArgumentException($"Type {componentType.Name} must be a Blazor component");
        }

        var modalReference = new BitModalReference(this, persistent);
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
            builder.OpenComponent<BitModal>(0);
            builder.SetKey(modalReference.Id);
            builder.AddComponentParameter(1, nameof(BitModal.IsOpen), true);
            builder.AddComponentParameter(2, nameof(BitModal.OnOverlayClick), EventCallback.Factory.Create<MouseEventArgs>(modalReference, () => modalReference.Close()));
            builder.AddComponentParameter(3, nameof(BitModal.ChildContent), content);
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
