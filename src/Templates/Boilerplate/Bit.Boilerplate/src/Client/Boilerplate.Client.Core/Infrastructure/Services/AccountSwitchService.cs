using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Client.Core.Infrastructure.Services;

public partial class AccountSwitchService
{
    [AutoInject] private AuthManager authManager = default!;
    [AutoInject] private PubSubService pubSubService = default!;
    [AutoInject] private BitModalService modalService = default!;
    [AutoInject] private SignInModalService signInModalService = default!;

    public async Task<bool> Switch(DeviceAccountDto account)
    {
        if (account.HasPin is false)
            return await authManager.SwitchAccount(account.UserId);

        TaskCompletionSource<bool> tcs = new();
        BitModalReference? modalReference = null;
        var signInAgain = false;

        void Complete(bool switched)
        {
            tcs.TrySetResult(switched);
            modalReference?.Close();
        }

        Dictionary<string, object> modalComponentParameters = new()
        {
            { nameof(AccountPinModal.Account), account },
            { nameof(AccountPinModal.OnResult), (Action<bool>)Complete },
            { nameof(AccountPinModal.OnSignInAgain), (Action)(() => { signInAgain = true; Complete(false); }) }
        };

        var modalParameters = new BitModalParameters
        {
            Draggable = true,
            TitleAriaId = "account-pin-title",
            DragElementSelector = ".header-stack",
            OnDismiss = EventCallback.Factory.Create<MouseEventArgs>(this, () => Complete(false))
        };

        modalReference = await modalService.Show<AccountPinModal>(modalComponentParameters, modalParameters);

        if (await tcs.Task)
            return true;

        if (signInAgain is false || await signInModalService.SignIn() is false)
            return false;

        pubSubService.Publish(ClientAppMessages.SOFT_RESTART);

        return true;
    }
}
