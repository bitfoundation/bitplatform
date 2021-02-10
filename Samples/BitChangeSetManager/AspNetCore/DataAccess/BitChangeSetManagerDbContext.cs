using Bit.Data.EntityFramework.Implementations;
using BitChangeSetManager.Model;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using Bit.Core.Contracts;
using Bit.Data.Contracts;
using Bit.Core.Models;

namespace BitChangeSetManager.DataAccess
{
    [DbConfigurationType(typeof(UseDefaultModelStoreDbConfiguration))]
    public class BitChangeSetManagerDbContext : EfDbContextBase
    {
        public BitChangeSetManagerDbContext(DbConnection existingConnection)
            : base(existingConnection, contextOwnsConnection: true)
        {

        }

        public BitChangeSetManagerDbContext(AppEnvironment appEnvironment, IDbConnectionProvider dbConnectionProvider)
            : base(appEnvironment.GetConfig<string>("BitChangeSetManagerDbConnectionString"), dbConnectionProvider)
        {

        }

        public virtual DbSet<ChangeSet> ChangeSets { get; set; }

        public virtual DbSet<Customer> Customers { get; set; }

        public virtual DbSet<Delivery> Deliveries { get; set; }

        public virtual DbSet<User> Users { get; set; }

        public virtual DbSet<ChangeSetSeverity> ChangeSetSeverities { get; set; }

        public virtual DbSet<ChangeSetDeliveryRequirement> ChangeSetDeliveryRequirements { get; set; }

        public virtual DbSet<Constant> Constants { get; set; }

        public virtual DbSet<ChangeSetImage> ChangeSetImages { get; set; }

        public virtual DbSet<Province> Provinces { get; set; }

        public virtual DbSet<City> Cities { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<ManyToManyCascadeDeleteConvention>();
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();

            modelBuilder.HasDefaultSchema("bit");

            base.OnModelCreating(modelBuilder);
        }
    }
}