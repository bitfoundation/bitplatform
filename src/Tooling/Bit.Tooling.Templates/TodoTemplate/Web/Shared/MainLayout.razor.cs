namespace TodoTemplate.App.Shared
{
    public partial class MainLayout : IAsyncDisposable
    {
        public bool IsUserAuthenticated { get; set; }

        [CascadingParameter]
        public Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

        [Inject]
        public IStateService StateService { get; set; } = default!;

        [Inject]
        public TodoTemplateAuthenticationStateProvider TodoTemplateAuthenticationStateProvider { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            TodoTemplateAuthenticationStateProvider.AuthenticationStateChanged += VerifyUserIsAuthenticatedOrNot;

            IsUserAuthenticated = await StateService.GetValue(nameof(IsUserAuthenticated), async () => (await AuthenticationStateTask).User.Identity?.IsAuthenticated == true);

            await base.OnInitializedAsync();
        }

        async void VerifyUserIsAuthenticatedOrNot(Task<AuthenticationState> task)
        {
            try
            {
                IsUserAuthenticated = await StateService.GetValue(nameof(IsUserAuthenticated), async () => (await AuthenticationStateTask).User.Identity?.IsAuthenticated == true);
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
