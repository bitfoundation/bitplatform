using Bit.CSharpClientSample.Data;
using Bit.Tests.Model.Dto;
using Bit.ViewModel;
using Bit.ViewModel.Contracts;
using Microsoft.EntityFrameworkCore;
using Prism.Navigation;
using Simple.OData.Client;
using System;
using System.Net.Http;

namespace Bit.CSharpClientSample.ViewModels
{
    public class MainViewModel : BitViewModelBase
    {
        public BitDelegateCommand Sync { get; set; }

        public BitDelegateCommand SendHttpRequest { get; set; }

        public BitDelegateCommand SendODataRequest { get; set; }

        public BitDelegateCommand Logout { get; set; }

        public MainViewModel(INavigationService navigationService, SampleDbContext dbContext, ISyncService syncService, IODataClient oDataClient, HttpClient httpClient, ISecurityService securityService)
        {
            SendHttpRequest = new BitDelegateCommand(async () =>
            {
                using (HttpResponseMessage response = await httpClient.GetAsync("odata/Test/parentEntities"))
                {
                }
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

            Sync = new BitDelegateCommand(async () =>
            {
                await dbContext.Database.EnsureDeletedAsync();
                await dbContext.Database.EnsureCreatedAsync();

                await oDataClient.For<TestCustomerDto>("TestCustomers")
                     .Set(new TestCustomerDto { Id = Guid.NewGuid(), Name = "A1", CityId = Guid.Parse("EF529174-C497-408B-BB4D-C31C205D46BB"), Kind = TestCustomerKind.Type1 })
                     .InsertEntryAsync();

                await oDataClient.For<TestCustomerDto>("TestCustomers")
                    .Set(new TestCustomerDto { Id = Guid.NewGuid(), Name = "A2", CityId = Guid.Parse("EF529174-C497-408B-BB4D-C31C205D46BB"), Kind = TestCustomerKind.Type1 })
                    .InsertEntryAsync();

                await syncService.SyncContext();

                TestCustomerDto customer = await dbContext.TestCustomers.FirstAsync();
                customer.Name += "?";
                dbContext.Update(customer);
                await dbContext.SaveChangesAsync();

                await oDataClient.For<TestCustomerDto>("TestCustomers")
                     .Set(new TestCustomerDto { Id = Guid.NewGuid(), Name = "A3", CityId = Guid.Parse("EF529174-C497-408B-BB4D-C31C205D46BB"), Kind = TestCustomerKind.Type1 })
                     .InsertEntryAsync();

                await oDataClient.For<TestCustomerDto>("TestCustomers")
                    .Set(new TestCustomerDto { Id = Guid.NewGuid(), Name = "A4", CityId = Guid.Parse("EF529174-C497-408B-BB4D-C31C205D46BB"), Kind = TestCustomerKind.Type1 })
                    .InsertEntryAsync();

                await syncService.SyncContext();

                await dbContext.Entry(customer).ReloadAsync();

                dbContext.Remove(customer);

                await dbContext.SaveChangesAsync();

                await syncService.SyncContext();
            });
        }
    }
}
