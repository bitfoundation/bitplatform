namespace TodoTemplate.App.Shared
{
    public partial class MainLayout : IAsyncDisposable
    {
        public bool IsUserAuthenticated { get; set; }
        public bool IsMenuOpen { get; set; } = false;

        [Inject]
        public IStateService StateService { get; set; } = default!;

        [Inject]
        public IExceptionHandler ExceptionHandler { get; set; } = default!;

        [Inject]
        public TodoTemplateAuthenticationStateProvider TodoTemplateAuthenticationStateProvider { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                TodoTemplateAuthenticationStateProvider.AuthenticationStateChanged += VerifyUserIsAuthenticatedOrNot;

                IsUserAuthenticated = await StateService.GetValue(nameof(IsUserAuthenticated), async () => await TodoTemplateAuthenticationStateProvider.IsUserAuthenticated());

                await base.OnInitializedAsync();
            }
            catch (Exception exp)
            {
                ExceptionHandler.OnExceptionReceived(exp);
            }
        }

        async void VerifyUserIsAuthenticatedOrNot(Task<AuthenticationState> task)
        {
            try
            {
                IsUserAuthenticated = await TodoTemplateAuthenticationStateProvider.IsUserAuthenticated();
            }
            catch (Exception ex)
            {
                ExceptionHandler.OnExceptionReceived(ex);
            }
            finally
            {
                StateHasChanged();
            }
        }

        private void ToggleMenuHandler()
        {
            IsMenuOpen = !IsMenuOpen;
        }

        public async ValueTask DisposeAsync()
        {
            TodoTemplateAuthenticationStateProvider.AuthenticationStateChanged -= VerifyUserIsAuthenticatedOrNot;
        }
    }
}
