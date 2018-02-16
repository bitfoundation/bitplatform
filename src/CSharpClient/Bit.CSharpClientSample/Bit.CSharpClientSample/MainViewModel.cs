using Bit.ViewModel.Contracts;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services;
using Simple.OData.Client;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Bit.CSharpClientSample
{
    public class MainViewModel : BindableBase
    {
        public DelegateCommand CheckIsLoggedIn { get; set; }

        public DelegateCommand LoginUsingResourceOwnerFlow { get; set; }

        public DelegateCommand LoginUsingImplicitFlow { get; set; }

        public DelegateCommand LoginUsingGooglePlus { get; set; }

        public string UserName { get; set; } = "ValidUserName";

        public string Password { get; set; } = "ValidPassword";

        public DelegateCommand SendHttpRequest { get; set; }

        public DelegateCommand SendODataRequest { get; set; }

        public DelegateCommand Logout { get; set; }

        public MainViewModel(ODataClient oDataClient, HttpClient httpClient, IPageDialogService pageDialogService, IDeviceService deviceService, ISecurityService securityService)
        {
            CheckIsLoggedIn = new DelegateCommand(() =>
            {
                bool isLoggedIn = securityService.IsLoggedIn();

                deviceService.BeginInvokeOnMainThread(async () =>
                {
                    await pageDialogService.DisplayAlertAsync("Is logged in", isLoggedIn ? "Yes" : "No", "Close");
                });
            });

            LoginUsingResourceOwnerFlow = new DelegateCommand(async () =>
            {
                await securityService.LoginWithCredentials(UserName, Password);
            }, () => !string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(Password));

            LoginUsingResourceOwnerFlow.ObservesProperty(() => UserName);
            LoginUsingResourceOwnerFlow.ObservesProperty(() => Password);

            LoginUsingImplicitFlow = new DelegateCommand(async () =>
            {
                await securityService.Login();
            });

            LoginUsingGooglePlus = new DelegateCommand(async () =>
            {
                await securityService.Login(new { SignInType = "Google" });
            });

            SendHttpRequest = new DelegateCommand(async () =>
            {
                HttpResponseMessage response = await httpClient.GetAsync("odata/Test/parentEntities");
            });

            SendODataRequest = new DelegateCommand(async () =>
            {
                var result = await oDataClient.For("ParentEntities").FindEntriesAsync();
            });

            Logout = new DelegateCommand(async () =>
            {
                await securityService.Logout();
            });
        }
    }
}
