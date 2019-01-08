using Bit.ViewModel;
using Bit.ViewModel.Contracts;
using Prism.Services;
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
                await SecurityService.LoginWithCredentials(UserName, Password, client_id: "TestResOwner", client_secret: "secret");
                await NavigationService.NavigateAsync("Main");
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
                await SecurityService.Login();
                await NavigationService.NavigateAsync("Main");
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
                await SecurityService.Login(new { SignInType = "Google" });
                await NavigationService.NavigateAsync("Main");
            }
            catch
            {
                await PageDialogService.DisplayAlertAsync("Login failed", "Login failed", "Ok");
                throw;
            }
        }

        async Task Skip()
        {
            await NavigationService.NavigateAsync("Main");
        }
    }
}
