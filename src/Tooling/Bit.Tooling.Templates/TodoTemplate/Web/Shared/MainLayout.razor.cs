using Microsoft.AspNetCore.Components.Authorization;

namespace TodoTemplate.App.Shared
{
    public partial class MainLayout : IAsyncDisposable
    {
        public bool UserIsAuthenticated { get; set; }

        [CascadingParameter]
        public Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

        [Inject]
        public TodoTemplateAuthenticationStateProvider TodoTemplateAuthenticationStateProvider { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            TodoTemplateAuthenticationStateProvider.AuthenticationStateChanged += VerifyUserIsAuthenticatedOrNot;

            VerifyUserIsAuthenticatedOrNot();

            await base.OnInitializedAsync();
        }

        async void VerifyUserIsAuthenticatedOrNot(Task<AuthenticationState>? task = null)
        {
            try
            {
                var authState = await AuthenticationStateTask;

                if (AuthenticationStateTask.Result.User.Identity != null)
                {
                    UserIsAuthenticated = authState.User.Identity!.IsAuthenticated;
                }
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
