using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Bit.Core.Contracts;
using Bit.Owin.Implementations;
using Bit.Tests.Model.DomainModels;
using Bit.Tests.Properties;
using Microsoft.EntityFrameworkCore;

namespace Bit.Tests.Data.Implementations
{
    public class InitialTestDataConfiguration : IAppEvents
    {
        private readonly IDependencyManager _dependencyManager;
        private readonly IDateTimeProvider _dateTimeProvider;

        public InitialTestDataConfiguration(IDependencyManager dependencyManager, IDateTimeProvider dateTimeProvider)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            if (dateTimeProvider == null)
                throw new ArgumentNullException(nameof(dateTimeProvider));

            _dependencyManager = dependencyManager;
            _dateTimeProvider = dateTimeProvider;

            _parentEntities = new List<ParentEntity>
            {
                new ParentEntity
                {
                    Name = "A",
                    Version = 1 ,
                    Date = _dateTimeProvider.GetCurrentUtcDateTime(),
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
                    Date = _dateTimeProvider.GetCurrentUtcDateTime(),
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
                    Date = _dateTimeProvider.GetCurrentUtcDateTime(),
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
            using (IDependencyResolver childDependencyResolver = _dependencyManager.CreateChildDependencyResolver())
            {
                TestDbContext dbContext = null;

                if (Settings.Default.UseInMemoryProviderByDefault)
                    dbContext = childDependencyResolver.Resolve<TestDbContext>();
                else
                {
                    DbContextOptionsBuilder dbContextOptionsBuilder = new DbContextOptionsBuilder();

                    SqlConnection dbConnection = new SqlConnection(DefaultAppEnvironmentProvider.Current.GetActiveAppEnvironment().GetConfig<string>("TestDbConnectionString"));

                    dbContextOptionsBuilder.UseSqlServer(dbConnection);

                    dbContext = new TestDbContext(dbContextOptionsBuilder.Options);
                }

                using (dbContext)
                {
                    dbContext.Database.EnsureDeleted();

                    dbContext.Database.EnsureCreated();

                    dbContext.TestModels.Add(_testModel);
                    dbContext.ParentEntities.AddRange(_parentEntities);

                    TestCity city = new TestCity { Id = Guid.Parse("EF529174-C497-408B-BB4D-C31C205D46BB"), Name = "TestCity" };
                    TestCustomer customer = new TestCustomer { Id = Guid.Parse("28E1FF65-DA41-4FA3-8AEB-5196494B407D"), City = city, CityId = city.Id, Name = "TestCustomer" };

                    dbContext.TestCities.Add(city);
                    dbContext.TestCustomers.Add(customer);

                    dbContext.SaveChanges();
                }
            }
        }

        public virtual void OnAppEnd()
        {
            using (IDependencyResolver childDependencyResolver = _dependencyManager.CreateChildDependencyResolver())
            {
                TestDbContext dbContext = childDependencyResolver.Resolve<TestDbContext>();

                dbContext.Database.EnsureDeleted();
            }
        }
    }
}