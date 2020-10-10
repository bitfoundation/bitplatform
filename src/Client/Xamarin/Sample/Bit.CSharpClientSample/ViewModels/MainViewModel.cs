using Bit.CSharpClientSample.Data;
using Bit.CSharpClientSample.Dto;
using Bit.Http.Contracts;
using Bit.Sync.ODataEntityFrameworkCore.Contracts;
using Bit.Tests.Model.Dto;
using Bit.ViewModel;
using Bit.ViewModel.Implementations;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Prism.Navigation;
using Prism.Regions;
using Prism.Regions.Navigation;
using Refit;
using Simple.OData.Client;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.CSharpClientSample.ViewModels
{
    public interface ISimpleApi
    {
        [Post("/api/customers/some-action")]
        Task<TestCustomerDto> SomeAction(TestCustomerDto customer, CancellationToken cancellationToken);
    }

    public class MainViewModel : BitViewModelBase
    {
        public SampleDbContext DbContext { get; set; }
        public ISyncService SyncService { get; set; }
        public IODataClient ODataClient { get; set; }
        public HttpClient HttpClient { get; set; }
        public ISecurityService SecurityService { get; set; }
        public ISimpleApi SimpleApi { get; set; }
        public LocalTelemetryService LocalTelemetryService { get; set; }

        public BitDelegateCommand SyncCommand { get; set; }
        public BitDelegateCommand SendHttpRequestCommand { get; set; }
        public BitDelegateCommand SendRefitRequestCommand { get; set; }
        public BitDelegateCommand SendODataRequestCommand { get; set; }
        public BitDelegateCommand SendODataBatchRequestCommand { get; set; }
        public BitDelegateCommand LogoutCommand { get; set; }
        public BitDelegateCommand ShowPopupCommand { get; set; }
        public BitDelegateCommand OpenConsoleCommand { get; set; }

        public MainViewModel()
        {
            SyncCommand = new BitDelegateCommand(Sync);
            SendHttpRequestCommand = new BitDelegateCommand(SendHttpRequest);
            SendRefitRequestCommand = new BitDelegateCommand(SendRefitRequest);
            SendODataRequestCommand = new BitDelegateCommand(SendODataRequest);
            SendODataBatchRequestCommand = new BitDelegateCommand(SendODataBatchRequest);
            LogoutCommand = new BitDelegateCommand(Logout);
            ShowPopupCommand = new BitDelegateCommand(ShowPopup);
            OpenConsoleCommand = new BitDelegateCommand(OpenConsole);
        }

        async Task OpenConsole()
        {
            await LocalTelemetryService.OpenConsole();
        }

        async Task Sync()
        {
            await DbContext.Database.EnsureDeletedAsync();
            await DbContext.Database.EnsureCreatedAsync();

            await ODataClient.For<TestCustomerDto>("TestCustomers")
                 .Set(new TestCustomerDto { Id = Guid.NewGuid(), Name = "A1", CityId = Guid.Parse("EF529174-C497-408B-BB4D-C31C205D46BB"), Kind = TestCustomerKind.Type1 })
                 .CreateEntryAsync();

            await ODataClient.For<TestCustomerDto>("TestCustomers")
                .Set(new TestCustomerDto { Id = Guid.NewGuid(), Name = "A2", CityId = Guid.Parse("EF529174-C497-408B-BB4D-C31C205D46BB"), Kind = TestCustomerKind.Type1 })
                .CreateEntryAsync();

            await SyncService.SyncContext();

            TestCustomerDto customer = await DbContext.TestCustomers.FirstAsync();
            customer.Name += "?";
            DbContext.Update(customer);
            await DbContext.SaveChangesAsync();

            await ODataClient.For<TestCustomerDto>("TestCustomers")
                 .Set(new TestCustomerDto { Id = Guid.NewGuid(), Name = "A3", CityId = Guid.Parse("EF529174-C497-408B-BB4D-C31C205D46BB"), Kind = TestCustomerKind.Type1 })
                 .CreateEntryAsync();

            await ODataClient.For<TestCustomerDto>("TestCustomers")
                .Set(new TestCustomerDto { Id = Guid.NewGuid(), Name = "A4", CityId = Guid.Parse("EF529174-C497-408B-BB4D-C31C205D46BB"), Kind = TestCustomerKind.Type1 })
                .CreateEntryAsync();

            await SyncService.SyncContext();

            await DbContext.Entry(customer).ReloadAsync();

            DbContext.Remove(customer);

            await DbContext.SaveChangesAsync();

            await SyncService.SyncContext();
        }

        async Task SendHttpRequest()
        {
            using (HttpResponseMessage response = await HttpClient.GetAsync("odata/v1/TestCustomers?$count=true"))
            {
                string responseAsString = await response.Content.ReadAsStringAsync();

                ODataResponse<TestCustomerDto[]> odataResponse = JsonConvert.DeserializeObject<ODataResponse<TestCustomerDto[]>>(responseAsString);

                foreach (TestCustomerDto customer in odataResponse.Value)
                {

                }
            }
        }

        async Task SendRefitRequest()
        {
            TestCustomerDto customer = await SimpleApi.SomeAction(new TestCustomerDto { Id = Guid.NewGuid(), Name = "Test" }, CancellationToken.None);
        }

        async Task SendODataRequest()
        {
            var result = await ODataClient.For("ParentEntities").FindEntriesAsync();
            var result2 = await ODataClient.For<TestComplexDto>("TestComplex")
                .FindEntriesAsync();
        }

        async Task SendODataBatchRequest()
        {
            var batchClient = new ODataBatch(ODataClient);

            IEnumerable<IDictionary<string, object>> result = null;
            IEnumerable<TestComplexDto> result2 = null;

            batchClient += async (c) => result = await c.For("ParentEntities").FindEntriesAsync();
            batchClient += async (c) => result2 = await c.For<TestComplexDto>("TestComplex").FindEntriesAsync();

            await batchClient.ExecuteAsync();
        }

        async Task Logout()
        {
            await SecurityService.Logout();
            await NavigationService.NavigateAsync("/Nav/Login");
        }

        async Task ShowPopup()
        {
            await NavigationService.NavigateAsync("Test", ("Test", "Test"));
        }

        public async override Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            await base.OnNavigatedToAsync(parameters);

            await RegionManager.RequestNavigateAsync("ContentRegion1", "RegionA", ("Parameter1", 1));
            await RegionManager.RequestNavigateAsync("ContentRegion2", "RegionC", ("Parameter1", 1));
        }

        public async override Task OnDestroyAsync()
        {
            await RegionManager.DestroyRegions("ContentRegion1", "ContentRegion2");

            await base.OnDestroyAsync();
        }
    }
}
