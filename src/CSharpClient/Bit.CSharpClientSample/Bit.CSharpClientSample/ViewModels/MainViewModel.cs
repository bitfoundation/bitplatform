using Bit.ViewModel;
using Bit.ViewModel.Contracts;
using Prism.Mvvm;
using Prism.Navigation;
using Simple.OData.Client;
using System.Net.Http;

namespace Bit.CSharpClientSample.ViewModels
{
    public class MainViewModel : BitViewModelBase
    {
        public BitDelegateCommand SendHttpRequest { get; set; }

        public BitDelegateCommand SendODataRequest { get; set; }

        public BitDelegateCommand Logout { get; set; }

        public MainViewModel(INavigationService navigationService, IODataClient oDataClient, HttpClient httpClient, ISecurityService securityService)
        {
            SendHttpRequest = new BitDelegateCommand(async () =>
            {
                HttpResponseMessage response = await httpClient.GetAsync("odata/Test/parentEntities");
            });

            SendODataRequest = new BitDelegateCommand(async () =>
            {
                var result = await oDataClient.For("ParentEntities").FindEntriesAsync();
            });

            Logout = new BitDelegateCommand(async () =>
            {
                await securityService.Logout();
                await navigationService.NavigateAsync("/Login");
            });
        }
    }
}
