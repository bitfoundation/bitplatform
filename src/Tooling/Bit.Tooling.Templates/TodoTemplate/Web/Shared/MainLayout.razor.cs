namespace TodoTemplate.App.Shared
{
    public partial class MainLayout : IAsyncDisposable
    {
        public bool IsUserAuthenticated { get; set; }

        [Inject]
        public IStateService StateService { get; set; } = default!;

        [Inject]
        public TodoTemplateAuthenticationStateProvider TodoTemplateAuthenticationStateProvider { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            TodoTemplateAuthenticationStateProvider.AuthenticationStateChanged += VerifyUserIsAuthenticatedOrNot;

            IsUserAuthenticated = await StateService.GetValue(nameof(IsUserAuthenticated), async () => await TodoTemplateAuthenticationStateProvider.IsUserAuthenticated());

            await base.OnInitializedAsync();
        }

        async void VerifyUserIsAuthenticatedOrNot(Task<AuthenticationState> task)
        {
            try
            {
                IsUserAuthenticated = await TodoTemplateAuthenticationStateProvider.IsUserAuthenticated();
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
