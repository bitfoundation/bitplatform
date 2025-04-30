using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Settings;

public partial class SettingsPage
{
    [Parameter] public string? Section { get; set; }


    [AutoInject] protected HttpClient HttpClient = default!;
    [AutoInject] private IUserController userController = default!;


    private UserDto? user;
    private bool isLoading;
    private string? openedAccordion;


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();
        
        openedAccordion = Section?.ToLower();

        isLoading = true;

        try
        {
            user = (await PrerenderStateService.GetValue(() => HttpClient.GetFromJsonAsync("api/User/GetCurrentUser", JsonSerializerOptions.GetTypeInfo<UserDto>(), CurrentCancellationToken)))!;
        }
        finally
        {
            isLoading = false;
        }
    }
}
