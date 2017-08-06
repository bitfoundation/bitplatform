using Bit.Core.Contracts;
using Bit.Data.EntityFrameworkCore.Contracts;
using Bit.Data.EntityFrameworkCore.Implementations;
using Bit.Model.DomainModels;
using Bit.Tests.Model.DomainModels;
using Microsoft.EntityFrameworkCore;

namespace Bit.Tests.Data.Implementations
{
    public class TestDbContext : EfCoreDbContextBase
    {
        protected TestDbContext()
        {

        }

        public TestDbContext(DbContextOptions options)
            : base(options)
        {
        }

        public TestDbContext(IAppEnvironmentProvider appEnvironmentProvider, IDbContextObjectsProvider dbContextCreationOptionsProvider)
              : base(appEnvironmentProvider.GetActiveAppEnvironment().GetConfig<string>("TestDbConnectionString"), dbContextCreationOptionsProvider)
        {

        }

        public virtual DbSet<UserSetting> UsersSettings { get; set; }

        public virtual DbSet<TestModel> TestModels { get; set; }

        public virtual DbSet<ParentEntity> ParentEntities { get; set; }

        public virtual DbSet<ChildEntity> ChildEntities { get; set; }

        public virtual DbSet<TestCustomer> TestCustomers { get; set; }

        public virtual DbSet<TestCity> TestCities { get; set; }
    }
}
