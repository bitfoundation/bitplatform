using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Client.Core.Infrastructure.Services;

/// <summary>
/// Opens the <see cref="ElevatedAccessModal"/> and reports whether the session was elevated. The code, the passkey
/// ceremony and the refresh all happen inside the modal (See <see cref="AuthManager.TryEnterElevatedAccessMode"/>).
/// </summary>
public partial class ElevatedAccessService
{
    [AutoInject] private BitModalService modalService = default!;

    public async Task<bool> Show(CancellationToken cancellationToken)
    {
        TaskCompletionSource<bool> tcs = new();
        BitModalReference? modalReference = null;

        void Complete(bool elevated)
        {
            tcs.TrySetResult(elevated);
            modalReference?.Close();
        }

        Dictionary<string, object> modalComponentParameters = new()
        {
            { nameof(ElevatedAccessModal.OnResult), (Action<bool>)Complete }
        };

        var modalParameters = new BitModalParameters
        {
            Draggable = true,
            DragElementSelector = ".header-stack",
            OnOverlayClick = EventCallback.Factory.Create<MouseEventArgs>(this, () => Complete(false))
        };

        modalReference = await modalService.Show<ElevatedAccessModal>(modalComponentParameters, modalParameters);

        // The page that asked to elevate can go away with the prompt still open, leaving nobody to answer it.
        await using var whenCallerGivesUp = cancellationToken.Register(() => Complete(false));

        return await tcs.Task;
    }
}
