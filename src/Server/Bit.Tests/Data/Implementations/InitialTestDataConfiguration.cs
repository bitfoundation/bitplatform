using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Tests.Model.DomainModels;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;

namespace Bit.Tests.Data.Implementations
{
    public class InitialTestDataConfiguration : IAppEvents
    {
        public virtual AppEnvironment AppEnvironment { get; set; }

        private readonly IDependencyManager _dependencyManager;

        public InitialTestDataConfiguration(IDependencyManager dependencyManager, IDateTimeProvider dateTimeProvider)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            if (dateTimeProvider == null)
                throw new ArgumentNullException(nameof(dateTimeProvider));

            _dependencyManager = dependencyManager;
            var dateTimeProvider1 = dateTimeProvider;

            _parentEntities = new List<ParentEntity>
            {
                new ParentEntity
                {
                    Name = "A",
                    Version = 1 ,
                    Date = dateTimeProvider1.GetCurrentUtcDateTime(),
                    ChildEntities = new List<ChildEntity>
                    {
                        new ChildEntity { Name = "a1" , Version = 7 },
                        new ChildEntity { Name = "a2" , Version = 8 },
                        new ChildEntity { Name = "a3" , Version = 9 }
                    }
                },
                new ParentEntity
                {
                    Name = "B",
                    Version = 2,
                    Date = dateTimeProvider1.GetCurrentUtcDateTime(),
                    ChildEntities = new List<ChildEntity>
                    {
                        new ChildEntity { Name = "b1" , Version = 4 },
                        new ChildEntity { Name = "b2" , Version = 5 },
                        new ChildEntity { Name = "b3" , Version = 6 }
                    }
                },
                new ParentEntity
                {
                    Name = "C",
                    Version = 3,
                    Date = dateTimeProvider1.GetCurrentUtcDateTime(),
                    ChildEntities = new List<ChildEntity>
                    {
                        new ChildEntity { Name = "c1" , Version = 1 },
                        new ChildEntity { Name = "c2" , Version = 2  },
                        new ChildEntity { Name = "c3" , Version = 3 }
                    }
                }
            };
        }

        protected InitialTestDataConfiguration()
        {

        }

        private readonly List<ParentEntity> _parentEntities;

        private readonly TestModel _testModel = new TestModel
        {
            StringProperty = "Test",
            DateProperty = new DateTimeOffset(2016, 1, 1, 10, 30, 0, TimeSpan.Zero),
            Version = 1
        };

        public virtual void OnAppStartup()
        {
            try
            {
                _dependencyManager.TransactionAction("DeleteDatabase", async childResolver =>
                {
                    TestDbContext dbContext = childResolver.Resolve<TestDbContext>();
                    dbContext.Database.EnsureDeleted(); // after delete, scope and it's transaction are not much usable!
                }).GetAwaiter().GetResult();
            }
            catch (SqlException)
            {
            }

            _dependencyManager.TransactionAction("CreateDatabaseWithInitialData", async childResolver =>
            {
                TestDbContext dbContext = childResolver.Resolve<TestDbContext>();

                dbContext.Database.EnsureCreated();

                dbContext.TestModels.Add(_testModel);
                dbContext.ParentEntities.AddRange(_parentEntities);

                TestCity city = new TestCity { Id = Guid.Parse("EF529174-C497-408B-BB4D-C31C205D46BB"), Name = "TestCity", Version = DateTimeOffset.UtcNow.Ticks };
                TestCustomer customer = new TestCustomer { Id = Guid.Parse("28E1FF65-DA41-4FA3-8AEB-5196494B407D"), City = city, CityId = city.Id, Name = "TestCustomer", Version = DateTimeOffset.UtcNow.Ticks };

                dbContext.TestCities.Add(city);
                dbContext.TestCustomers.Add(customer);

                dbContext.SaveChanges();
            }).GetAwaiter().GetResult();
        }

        public virtual void OnAppEnd()
        {
            _dependencyManager.TransactionAction("DeleteDatabase", async childResolver =>
            {
                TestDbContext dbContext = childResolver.Resolve<TestDbContext>();

                dbContext.Database.EnsureDeleted();
            }).GetAwaiter().GetResult();
        }
    }
}
