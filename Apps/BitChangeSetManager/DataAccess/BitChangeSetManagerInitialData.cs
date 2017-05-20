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

                usersRepository.Add(new User { Id = Guid.NewGuid(), UserName = "Test1", Password = password, Culture = BitCulture.EnUs });
                usersRepository.Add(new User { Id = Guid.NewGuid(), UserName = "Test2", Password = password, Culture = BitCulture.FaIr });
                usersRepository.Add(new User { Id = Guid.NewGuid(), UserName = "Test3", Password = password, Culture = BitCulture.EnUs });
                usersRepository.Add(new User { Id = Guid.NewGuid(), UserName = "Test4", Password = password, Culture = BitCulture.FaIr });

                IBitChangeSetManagerRepository<Customer> customersRepository = childResolver.Resolve<IBitChangeSetManagerRepository<Customer>>();
                IBitChangeSetManagerRepository<Delivery> deliveriesRepository = childResolver.Resolve<IBitChangeSetManagerRepository<Delivery>>();
                IChangeSetRepository changeSetsRepository = childResolver.Resolve<IChangeSetRepository>();
                IBitChangeSetManagerRepository<ChangeSetSeverity> changeSetSeverities = childResolver.Resolve<IBitChangeSetManagerRepository<ChangeSetSeverity>>();
                IBitChangeSetManagerRepository<ChangeSetDeliveryRequirement> changeSetDeliveryRequirements = childResolver.Resolve<IBitChangeSetManagerRepository<ChangeSetDeliveryRequirement>>();

                Customer customer1 = new Customer { Id = Guid.NewGuid(), Name = "Customer1" };
                Customer customer2 = new Customer { Id = Guid.NewGuid(), Name = "Customer2" };

                ChangeSetDeliveryRequirement changeSetDeliveryRequirement1 = new ChangeSetDeliveryRequirement { Id = Guid.NewGuid(), Title = "Deliver to all developers" };
                ChangeSetDeliveryRequirement changeSetDeliveryRequirement2 = new ChangeSetDeliveryRequirement { Id = Guid.NewGuid(), Title = "Deliver to technical manager" };
                ChangeSetDeliveryRequirement changeSetDeliveryRequirement3 = new ChangeSetDeliveryRequirement { Id = Guid.NewGuid(), Title = "No specific delivery is required" };

                ChangeSetSeverity changeSetSeverity1 = new ChangeSetSeverity { Id = Guid.NewGuid(), Title = "Low" };
                ChangeSetSeverity changeSetSeverity2 = new ChangeSetSeverity { Id = Guid.NewGuid(), Title = "Medium" };
                ChangeSetSeverity changeSetSeverity3 = new ChangeSetSeverity { Id = Guid.NewGuid(), Title = "High" };

                ChangeSet changeSet1 = new ChangeSet { Id = Guid.NewGuid(), AssociatedCommitUrl = "http://github.com/bit-foundation/bit-framework", Description = "Desc1", Title = "ChangeSet1", DeliveryRequirementId = changeSetDeliveryRequirement1.Id, SeverityId = changeSetSeverity3.Id };
                ChangeSet changeSet2 = new ChangeSet { Id = Guid.NewGuid(), AssociatedCommitUrl = "http://github.com/bit-foundation/bit-framework", Description = "Desc2", Title = "ChangeSet2", DeliveryRequirementId = changeSetDeliveryRequirement1.Id, SeverityId = changeSetSeverity3.Id };

                Delivery delivery1 = new Delivery { ChangeSetId = changeSet1.Id, Id = Guid.NewGuid(), CustomerId = customer1.Id, DeliveredOn = _dateTimeProvider.GetCurrentUtcDateTime() };
                Delivery delivery2 = new Delivery { ChangeSetId = changeSet1.Id, Id = Guid.NewGuid(), CustomerId = customer2.Id, DeliveredOn = _dateTimeProvider.GetCurrentUtcDateTime() };
                Delivery delivery3 = new Delivery { ChangeSetId = changeSet2.Id, Id = Guid.NewGuid(), CustomerId = customer1.Id, DeliveredOn = _dateTimeProvider.GetCurrentUtcDateTime() };

                customersRepository.AddRange(new[] { customer1, customer2 });
                changeSetSeverities.AddRange(new[] { changeSetSeverity1, changeSetSeverity2, changeSetSeverity3 });
                changeSetDeliveryRequirements.AddRange(new[] { changeSetDeliveryRequirement1, changeSetDeliveryRequirement2, changeSetDeliveryRequirement3 });
                changeSetsRepository.AddRange(new[] { changeSet1, changeSet2 });
                deliveriesRepository.AddRange(new[] { delivery1, delivery2, delivery3 });
            }
        }
    }
}