using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public class BitModalService
{
    public event Func<BitModalReference, Task>? OnAddModal;

    public event Action<BitModalReference>? OnCloseModal;



    public async Task<BitModalReference> Show<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(IReadOnlyDictionary<string, object>? parameters = null)
    {
        var componentType = typeof(T);
        var modalReference = new BitModalReference(this);

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

    public void Close(BitModalReference modal)
    {
        OnCloseModal?.Invoke(modal);
    }
}
