using Microsoft.EntityFrameworkCore;

namespace Bit.Data.EntityFrameworkCore.Implementations
{
    public class EfCoreDbContextBase : DbContext
    {
        public EfCoreDbContextBase()
        {
        }

        public EfCoreDbContextBase(DbContextOptions options)
            : base(options)
        {
            
        }

        public virtual bool ChangeTrackingEnabled()
        {
            return true;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (ChangeTrackingEnabled() == false)
            {
                optionsBuilder = optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            }

            base.OnConfiguring(optionsBuilder);
        }
    }
}
