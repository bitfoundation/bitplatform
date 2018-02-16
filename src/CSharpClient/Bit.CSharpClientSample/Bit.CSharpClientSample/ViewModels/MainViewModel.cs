using Prism.Commands;
using Prism.Mvvm;
using Simple.OData.Client;
using System.Net.Http;

namespace Bit.CSharpClientSample.ViewModels
{
    public class MainViewModel : BindableBase
    {
        public DelegateCommand SendHttpRequest { get; set; }

        public DelegateCommand SendODataRequest { get; set; }

        public MainViewModel(ODataClient oDataClient, HttpClient httpClient)
        {
            SendHttpRequest = new DelegateCommand(async () =>
            {
                HttpResponseMessage response = await httpClient.GetAsync("odata/Test/parentEntities");
            });

            SendODataRequest = new DelegateCommand(async () =>
            {
                var result = await oDataClient.For("ParentEntities").FindEntriesAsync();
            });
        }
    }
}
