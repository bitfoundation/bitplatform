using Microsoft.AspNetCore.Components.Authorization;

namespace TodoTemplate.App.Shared
{
    public partial class MainLayout : IAsyncDisposable
    {
        public bool UserIsAuthenticated { get; set; }

        [CascadingParameter]
        public Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

        [Inject]
        public StateService StateService { get; set; } = default!;

        [Inject]
        public TodoTemplateAuthenticationStateProvider TodoTemplateAuthenticationStateProvider { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            TodoTemplateAuthenticationStateProvider.AuthenticationStateChanged += VerifyUserIsAuthenticatedOrNot;

            UserIsAuthenticated = await StateService.GetValue(nameof(UserIsAuthenticated), async () => (await AuthenticationStateTask).User.Identity?.IsAuthenticated == true);

            await base.OnInitializedAsync();
        }

        async void VerifyUserIsAuthenticatedOrNot(Task<AuthenticationState> task)
        {
            try
            {
                UserIsAuthenticated = await StateService.GetValue(nameof(UserIsAuthenticated), async () => (await AuthenticationStateTask).User.Identity?.IsAuthenticated == true);
            }
            finally
            {
                StateHasChanged();
            }
        }

        public async ValueTask DisposeAsync()
        {
            TodoTemplateAuthenticationStateProvider.AuthenticationStateChanged -= VerifyUserIsAuthenticatedOrNot;
        }
    }
}
