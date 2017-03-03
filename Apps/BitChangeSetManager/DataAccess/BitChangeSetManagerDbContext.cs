using BitChangeSetManager.Model;
using Foundation.DataAccess.Contracts.EntityFrameworkCore;
using Foundation.DataAccess.Implementations.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BitChangeSetManager.DataAccess
{
    public class BitChangeSetManagerDbContext : DefaultDbContext
    {
        public BitChangeSetManagerDbContext(DbContextOptions options) 
            : base(options)
        {

        }

        public BitChangeSetManagerDbContext(IDbContextObjectsProvider dbContextCreationOptionsProvider)
            : base("BitChangeSetManagerDbConnectionString", dbContextCreationOptionsProvider)
        {

        }

        public virtual DbSet<ChangeSet> ChangeSets { get; set; }

        public virtual DbSet<Customer> Customers { get; set; }

        public virtual DbSet<Delivery> Deliveries { get; set; }
    }
}