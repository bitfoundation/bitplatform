using Bit.ViewModel;
using Bit.ViewModel.Contracts;
using Prism.Navigation;
using Prism.Services;
using Simple.OData.Client;
using System.Net.Http;

namespace Bit.CSharpClientSample.ViewModels
{
    public class LoginViewModel : BitViewModelBase
    {
        public BitDelegateCommand LoginUsingCredentionals { get; set; }

        public BitDelegateCommand LoginUsingBrowser { get; set; }

        public BitDelegateCommand LoginUsingGooglePlus { get; set; }

        public BitDelegateCommand Skip { get; set; }

        public string UserName { get; set; } = "ValidUserName";

        public string Password { get; set; } = "ValidPassword";

        public LoginViewModel(INavigationService navigationService, IODataClient oDataClient, HttpClient httpClient, IPageDialogService pageDialogService, ISecurityService securityService)
        {
            LoginUsingCredentionals = new BitDelegateCommand(async () =>
            {
                try
                {
                    await securityService.LoginWithCredentials(UserName, Password, client_id: "TestResOwner", client_secret: "secret");
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

            LoginUsingGooglePlus = new BitDelegateCommand(async () =>
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

            LoginUsingBrowser = new BitDelegateCommand(async () =>
            {
                try
                {
                    await securityService.Login();
                    await navigationService.NavigateAsync("/Nav/Main");
                }
                catch
                {
                    await pageDialogService.DisplayAlertAsync("Login failed", "Login failed", "Ok");
                    throw;
                }
            });

            Skip = new BitDelegateCommand(async () =>
            {
                await navigationService.NavigateAsync("/Nav/Main");
            });
        }
    }
}
