using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public class BitModalService
{
    /// <summary>
    /// The event for when a new modal gets added through calling the Show method.
    /// </summary>
    public event Func<BitModalReference, Task>? OnAddModal;

    /// <summary>
    /// The event for when a modal gets removed through calling the Close method.
    /// </summary>
    public event Func<BitModalReference, Task>? OnCloseModal;



    /// <summary>
    /// Closes an already opened modal using its reference.
    /// </summary>
    /// <param name="modal"></param>
    public async Task Close(BitModalReference modal)
    {
        var modalClose = OnCloseModal;
        if (modalClose is not null)
        {
            await modalClose(modal);
        }
    }

    /// <summary>
    /// Shows a new BitModal with a custom component with parameters as its content.
    /// </summary>
    public Task<BitModalReference> Show<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        Dictionary<string, object>? parameters = null)
    {
        return Show<T>(parameters, null);
    }

    /// <summary>
    /// Shows a new BitModal with a custom component as its content with custom parameters for the modal.
    /// </summary>
    public Task<BitModalReference> Show<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        BitModalParameters? modalParameters = null)
    {
        return Show<T>(null, modalParameters);
    }

    /// <summary>
    /// Shows a new BitModal with a custom component as its content with custom parameters for the custom component and the modal.
    /// </summary>
    public async Task<BitModalReference> Show<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        Dictionary<string, object>? parameters,
        BitModalParameters? modalParameters)
    {
        var componentType = typeof(T);

        if (typeof(IComponent).IsAssignableFrom(componentType) is false)
        {
            throw new ArgumentException($"Type {componentType.Name} must be a Blazor component");
        }

        var modalReference = new BitModalReference(this);
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

        return modalReference;
    }
}
