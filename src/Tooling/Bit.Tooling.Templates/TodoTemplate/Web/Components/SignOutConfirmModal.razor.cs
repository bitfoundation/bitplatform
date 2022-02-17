namespace TodoTemplate.App.Components
{
    public partial class SignOutConfirmModal
    {
        [Inject]
        public ITodoTemplateAuthenticationService TodoTemplateAuthenticationService { get; set; } = default!;

        [Parameter]
        public bool IsOpen { get; set; }

        [Parameter]
        public EventCallback ModalClosed { get; set; }

        private async Task CloseModal()
        {
            IsOpen = false;
            await ModalClosed.InvokeAsync();
        }

        private async Task SignOut()
        {
            await TodoTemplateAuthenticationService.SignOut();
            await CloseModal();
        }
    }
}
