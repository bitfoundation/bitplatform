using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Settings.Account;

public partial class AccountSection
{
    [Parameter] public UserDto? User { get; set; }


    [AutoInject] private WebAuthn webAuthn = default!;


    private bool showPasswordless;


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        if (InPrerenderSession is false)
        {
            showPasswordless = await webAuthn.IsAvailable();
        }
    }
}
