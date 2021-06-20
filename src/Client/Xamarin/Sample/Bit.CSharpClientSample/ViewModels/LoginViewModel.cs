using Bit.Http.Contracts;
using Bit.ViewModel;
using Prism.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bit.CSharpClientSample.ViewModels
{
    public class LoginViewModel : BitViewModelBase
    {
        public ISecurityService SecurityService { get; set; }
        public IPageDialogService PageDialogService { get; set; }

        public BitDelegateCommand LoginUsingCredentialsCommand { get; set; }
        public BitDelegateCommand LoginUsingBrowserCommand { get; set; }
        public BitDelegateCommand LoginUsingGooglePlusCommand { get; set; }
        public BitDelegateCommand SkipCommand { get; set; }

        public LoginViewModel()
        {
            LoginUsingCredentialsCommand = new BitDelegateCommand(LoginUsingCredentials, () => !string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(Password));
            LoginUsingCredentialsCommand.ObservesProperty(() => UserName);
            LoginUsingCredentialsCommand.ObservesProperty(() => Password);
            LoginUsingBrowserCommand = new BitDelegateCommand(LoginUsingBrowser);
            LoginUsingGooglePlusCommand = new BitDelegateCommand(LoginUsingGooglePlus);
            SkipCommand = new BitDelegateCommand(Skip);
        }

        public string UserName { get; set; } = "ValidUserName";
        public string Password { get; set; } = "ValidPassword";

        async Task LoginUsingCredentials()
        {
            try
            {
                await SecurityService.LoginWithCredentials(UserName, Password, client_id: "TestResOwner", client_secret: "secret", acr_values: new Dictionary<string, string> { { "x", "1" } });
                await NavigationService.NavigateAsync("/Nav/Main");
            }
            catch
            {
                await PageDialogService.DisplayAlertAsync("Login failed", "Login failed", "Ok");
                throw;
            }
        }

        async Task LoginUsingBrowser()
        {
            try
            {
                await SecurityService.Login(acr_values: new Dictionary<string, string> { { "x", "1" } });
                await NavigationService.NavigateAsync("/Nav/Main");
            }
            catch
            {
                await PageDialogService.DisplayAlertAsync("Login failed", "Login failed", "Ok");
                throw;
            }
        }

        async Task LoginUsingGooglePlus()
        {
            try
            {
                await SecurityService.Login(new { SignInType = "Google" }, acr_values: new Dictionary<string, string> { { "x", "1" } });
                await NavigationService.NavigateAsync("/Nav/Main");
            }
            catch
            {
                await PageDialogService.DisplayAlertAsync("Login failed", "Login failed", "Ok");
                throw;
            }
        }

        async Task Skip()
        {
            await NavigationService.NavigateAsync("/Nav/Main");
        }
    }
}
