using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Client.Core.Infrastructure.Services;

public partial class AccountSwitchService
{
    [AutoInject] private AuthManager authManager = default!;
    [AutoInject] private PubSubService pubSubService = default!;
    [AutoInject] private BitModalService modalService = default!;
    [AutoInject] private SignInModalService signInModalService = default!;

    public async Task<bool> Switch(DeviceUserSessionDto userSession)
    {
        if (userSession.HasPin is false)
            return await authManager.SwitchAccount(userSession.UserId);

        var signInAgain = false;

        var switched = await ShowModal<UnlockAccountModal, bool>("unlock-account-title", complete => new()
        {
            { nameof(UnlockAccountModal.UserSession), userSession },
            { nameof(UnlockAccountModal.OnResult), complete },
            { nameof(UnlockAccountModal.OnSignInAgain), (Action)(() => { signInAgain = true; complete(false); }) }
        });

        if (switched)
            return true;

        if (signInAgain is false || await signInModalService.SignIn() is false)
            return false;

        pubSubService.Publish(ClientAppMessages.SOFT_RESTART);

        return true;
    }

    public async Task<bool> Lock(CancellationToken cancellationToken)
    {
        if (await authManager.HasPin() is false)
        {
            var pin = await ShowModal<LockAccountModal, string?>("lock-account-title", complete => new()
            {
                { nameof(LockAccountModal.OnResult), complete }
            });

            if (pin is null || await authManager.TryEnterElevatedAccessMode(cancellationToken) is false)
                return false;

            await authManager.SetPin(pin);
        }

        await authManager.Lock();

        return true;
    }

    private async Task<TResult?> ShowModal<TModal, TResult>(string titleAriaId, Func<Action<TResult?>, Dictionary<string, object>> getParameters)
        where TModal : IComponent
    {
        TaskCompletionSource<TResult?> tcs = new();
        BitModalReference? modalReference = null;

        void Complete(TResult? result)
        {
            tcs.TrySetResult(result);
            modalReference?.Close();
        }

        var modalParameters = new BitModalParameters
        {
            Draggable = true,
            TitleAriaId = titleAriaId,
            DragElementSelector = ".header-stack",
            OnDismiss = EventCallback.Factory.Create<MouseEventArgs>(this, () => Complete(default))
        };

        modalReference = await modalService.Show<TModal>(getParameters(Complete), modalParameters);

        return await tcs.Task;
    }
}
