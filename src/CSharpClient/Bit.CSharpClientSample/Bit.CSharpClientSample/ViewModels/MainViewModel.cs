using Bit.ViewModel.Contracts;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation;
using Simple.OData.Client;
using System.Net.Http;

namespace Bit.CSharpClientSample.ViewModels
{
    public class MainViewModel : BindableBase
    {
        public DelegateCommand SendHttpRequest { get; set; }

        public DelegateCommand SendODataRequest { get; set; }
        public DelegateCommand Logout { get; set; }

        public MainViewModel(INavigationService navigationService, ODataClient oDataClient, HttpClient httpClient, ISecurityService securityService)
        {
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
                await navigationService.NavigateAsync("/Login");
            });
        }
    }
}
