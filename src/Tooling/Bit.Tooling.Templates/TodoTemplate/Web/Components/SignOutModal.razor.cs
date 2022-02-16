namespace TodoTemplate.App.Components
{
    public partial class SignOutModal: TodoTemplateComponentBase
    {
        [Inject]
        public ITodoTemplateAuthenticationService TodoTemplateAuthenticationService { get; set; } = default!;

        [Parameter]
        public bool IsOpen { get; set; }

        [Parameter]
        public EventCallback ModalClosed { get; set; }

        private async void CloseModal()
        {
            IsOpen = false;
            await ModalClosed.InvokeAsync();
        }

        private void SignOut()
        {
            TodoTemplateAuthenticationService.SignOut();
            CloseModal();
        }
    }
}
