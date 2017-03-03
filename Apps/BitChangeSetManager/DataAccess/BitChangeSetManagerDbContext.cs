using Foundation.DataAccess.Contracts.EntityFrameworkCore;
using Foundation.DataAccess.Implementations.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BitChangeSetManager
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
    }
}