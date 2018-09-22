using Bit.CSharpClientSample.Data;
using Bit.CSharpClientSample.Dto;
using Bit.Tests.Model.Dto;
using Bit.ViewModel;
using Bit.ViewModel.Contracts;
using Microsoft.EntityFrameworkCore;
using Prism.Navigation;
using Simple.OData.Client;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Bit.CSharpClientSample.ViewModels
{
    public class MainViewModel : BitViewModelBase
    {
        public SampleDbContext DbContext { get; set; }
        public ISyncService SyncService { get; set; }
        public IODataClient ODataClient { get; set; }
        public HttpClient HttpClient { get; set; }
        public ISecurityService SecurityService { get; set; }
        public INavigationService NavigationService { get; set; }

        public BitDelegateCommand SyncCommand { get; set; }
        public BitDelegateCommand SendHttpRequestCommand { get; set; }
        public BitDelegateCommand SendODataRequestCommand { get; set; }
        public BitDelegateCommand LogoutCommand { get; set; }
        public BitDelegateCommand ShowPopupCommand { get; set; }

        public MainViewModel()
        {
            SyncCommand = new BitDelegateCommand(Sync);
            SendHttpRequestCommand = new BitDelegateCommand(SendHttpRequest);
            SendODataRequestCommand = new BitDelegateCommand(SendODataRequest);
            LogoutCommand = new BitDelegateCommand(Logout);
            ShowPopupCommand = new BitDelegateCommand(ShowPopup);
        }

        async Task Sync()
        {
            await DbContext.Database.EnsureDeletedAsync();
            await DbContext.Database.EnsureCreatedAsync();

            await ODataClient.For<TestCustomerDto>("TestCustomers")
                 .Set(new TestCustomerDto { Id = Guid.NewGuid(), Name = "A1", CityId = Guid.Parse("EF529174-C497-408B-BB4D-C31C205D46BB"), Kind = TestCustomerKind.Type1 })
                 .InsertEntryAsync();

            await ODataClient.For<TestCustomerDto>("TestCustomers")
                .Set(new TestCustomerDto { Id = Guid.NewGuid(), Name = "A2", CityId = Guid.Parse("EF529174-C497-408B-BB4D-C31C205D46BB"), Kind = TestCustomerKind.Type1 })
                .InsertEntryAsync();

            await SyncService.SyncContext();

            TestCustomerDto customer = await DbContext.TestCustomers.FirstAsync();
            customer.Name += "?";
            DbContext.Update(customer);
            await DbContext.SaveChangesAsync();

            await ODataClient.For<TestCustomerDto>("TestCustomers")
                 .Set(new TestCustomerDto { Id = Guid.NewGuid(), Name = "A3", CityId = Guid.Parse("EF529174-C497-408B-BB4D-C31C205D46BB"), Kind = TestCustomerKind.Type1 })
                 .InsertEntryAsync();

            await ODataClient.For<TestCustomerDto>("TestCustomers")
                .Set(new TestCustomerDto { Id = Guid.NewGuid(), Name = "A4", CityId = Guid.Parse("EF529174-C497-408B-BB4D-C31C205D46BB"), Kind = TestCustomerKind.Type1 })
                .InsertEntryAsync();

            await SyncService.SyncContext();

            await DbContext.Entry(customer).ReloadAsync();

            DbContext.Remove(customer);

            await DbContext.SaveChangesAsync();

            await SyncService.SyncContext();
        }

        async Task SendHttpRequest()
        {
            using (HttpResponseMessage response = await HttpClient.GetAsync("odata/Test/parentEntities"))
            {
            }
        }

        async Task SendODataRequest()
        {
            var result = await ODataClient.For("ParentEntities").FindEntriesAsync();
            var result2 = await ODataClient.For<TestComplexDto>("TestComplex")
                .FindEntriesAsync();
        }

        async Task Logout()
        {
            await SecurityService.Logout();
            await NavigationService.NavigateAsync("/Login");
        }

        async Task ShowPopup()
        {
            await NavigationService.NavigateAsync("Test", new NavigationParameters { { "Test", "Test" } });
        }
    }
}
