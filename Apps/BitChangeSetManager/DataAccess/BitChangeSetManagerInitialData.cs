using BitChangeSetManager.Model;
using Foundation.Core.Contracts;
using System;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace BitChangeSetManager.DataAccess
{
    public class BitChangeSetManagerInitialData : IAppEvents
    {
        private readonly IAppEnvironmentProvider _appEnvironmentProvider;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IDependencyManager _dependencyManager;

        public BitChangeSetManagerInitialData(IDependencyManager dependencyManager, IAppEnvironmentProvider appEnvironmentProvider, IDateTimeProvider dateTimeProvider)
        {
            _dependencyManager = dependencyManager;
            _dateTimeProvider = dateTimeProvider;
            _appEnvironmentProvider = appEnvironmentProvider;
        }

        public void OnAppEnd()
        {

        }

        public void OnAppStartup()
        {
            Database.SetInitializer<BitChangeSetManagerDbContext>(null);

            using (IDependencyResolver childResolver = _dependencyManager.CreateChildDependencyResolver())
            {
                using (SqlConnection dbConnection = new SqlConnection(_appEnvironmentProvider.GetActiveAppEnvironment().GetConfig<string>("BitChangeSetManagerDbConnectionString")))
                {
                    using (BitChangeSetManagerDbContext dbContext = new BitChangeSetManagerDbContext(dbConnection))
                    {
                        bool newDbCreated = dbContext.Database.CreateIfNotExists();

                        if (newDbCreated == false)
                            return;
                    }

                }

                IBitChangeSetManagerRepository<User> usersRepository = childResolver.Resolve<IBitChangeSetManagerRepository<User>>();

                string password = "test";

                using (MD5 md5Hash = MD5.Create())
                {
                    byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

                    StringBuilder sBuilder = new StringBuilder();

                    for (int i = 0; i < data.Length; i++)
                    {
                        sBuilder.Append(data[i].ToString("x2"));
                    }

                    password = sBuilder.ToString();
                }

                usersRepository.Add(new User { Id = Guid.NewGuid(), UserName = "Test", Password = password });

                IBitChangeSetManagerRepository<Customer> customersRepository = childResolver.Resolve<IBitChangeSetManagerRepository<Customer>>();
                IBitChangeSetManagerRepository<Delivery> deliveriesRepository = childResolver.Resolve<IBitChangeSetManagerRepository<Delivery>>();
                IBitChangeSetManagerRepository<ChangeSet> changeSetsRepository = childResolver.Resolve<IBitChangeSetManagerRepository<ChangeSet>>();

                Customer customer1 = new Customer { Id = Guid.NewGuid(), Name = "Customer1" };
                Customer customer2 = new Customer { Id = Guid.NewGuid(), Name = "Customer2" };

                ChangeSet changeSet1 = new ChangeSet { Id = Guid.NewGuid(), AssociatedCommitUrl = "http://github.com/bit-foundation/bit-framework", CreatedOn = _dateTimeProvider.GetCurrentUtcDateTime(), Description = "Desc1", Title = "ChangeSet1" };
                ChangeSet changeSet2 = new ChangeSet { Id = Guid.NewGuid(), AssociatedCommitUrl = "http://github.com/bit-foundation/bit-framework", CreatedOn = _dateTimeProvider.GetCurrentUtcDateTime(), Description = "Desc2", Title = "ChangeSet2" };

                Delivery delivery1 = new Delivery { ChangeSetId = changeSet1.Id, Id = Guid.NewGuid(), CustomerId = customer1.Id, DeliveredOn = _dateTimeProvider.GetCurrentUtcDateTime() };
                Delivery delivery2 = new Delivery { ChangeSetId = changeSet1.Id, Id = Guid.NewGuid(), CustomerId = customer2.Id, DeliveredOn = _dateTimeProvider.GetCurrentUtcDateTime() };
                Delivery delivery3 = new Delivery { ChangeSetId = changeSet2.Id, Id = Guid.NewGuid(), CustomerId = customer1.Id, DeliveredOn = _dateTimeProvider.GetCurrentUtcDateTime() };

                customersRepository.AddRange(new[] { customer1, customer2 });
                changeSetsRepository.AddRange(new[] { changeSet1, changeSet2 });
                deliveriesRepository.AddRange(new[] { delivery1, delivery2, delivery3 });
            }
        }
    }
}