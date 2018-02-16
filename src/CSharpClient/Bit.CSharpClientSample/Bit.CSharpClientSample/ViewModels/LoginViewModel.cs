using Bit.ViewModel.Contracts;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation;
using Prism.Services;
using Simple.OData.Client;
using System.Net.Http;

namespace Bit.CSharpClientSample.ViewModels
{
    public class LoginViewModel : BindableBase
    {
        public DelegateCommand LoginUsingCredentionals { get; set; }

        public DelegateCommand LoginUsingGooglePlus { get; set; }

        public string UserName { get; set; } = "ValidUserName";

        public string Password { get; set; } = "ValidPassword";

        public LoginViewModel(INavigationService navigationService, ODataClient oDataClient, HttpClient httpClient, IPageDialogService pageDialogService, ISecurityService securityService)
        {
            LoginUsingCredentionals = new DelegateCommand(async () =>
            {
                try
                {
                    await securityService.LoginWithCredentials(UserName, Password);
                    await navigationService.NavigateAsync("/Nav/Main");
                }
                catch
                {
                    await pageDialogService.DisplayAlertAsync("Login failed", "Login failed", "Ok");
                    throw;
                }
            }, () => !string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(Password));

            LoginUsingCredentionals.ObservesProperty(() => UserName);
            LoginUsingCredentionals.ObservesProperty(() => Password);

            LoginUsingGooglePlus = new DelegateCommand(async () =>
            {
                try
                {
                    await securityService.Login(new { SignInType = "Google" });
                    await navigationService.NavigateAsync("/Nav/Main");
                }
                catch
                {
                    await pageDialogService.DisplayAlertAsync("Login failed", "Login failed", "Ok");
                    throw;
                }
            });
        }
    }
}
