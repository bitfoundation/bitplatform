using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bit.Http.Contracts;

namespace Bit.MauiAppSample.ViewModels
{
    public class LoginViewModel : SampleViewModelBase
    {
        public ISecurityService SecurityService {  get; set; }

        public string UserName { get; set; } = "ValidUserName";
        public string Password { get; set; } = "ValidPassword";

        public async Task Login()
        {
            await SecurityService.LoginWithCredentials(UserName, Password, client_id: "TestResOwner", client_secret: "secret", acr_values: new Dictionary<string, string> { { "x", "1" } });
            RedemptionAuthenticationStateProvider.StateHasChanged();
            NavigationManager.NavigateTo("/");
        }
    }
}
