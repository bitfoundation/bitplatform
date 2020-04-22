using Bit.Model.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Data.EntityFrameworkCore.Implementations
{
    public interface IsSyncDbContext
    {
        bool IsSyncDbContext { get; set; }
    }

    public class EfCoreDbContextBase : DbContext, IsSyncDbContext
    {
        bool IsSyncDbContext.IsSyncDbContext { get; set; }

        public EfCoreDbContextBase()
        {
        }

        public EfCoreDbContextBase(DbContextOptions options)
            : base(options)
        {

        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            OnSaveChanges();

            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            OnSaveChanges();

            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        protected virtual void OnSaveChanges()
        {
            ChangeTracker.DetectChanges();

            if (((IsSyncDbContext)this).IsSyncDbContext == false)
            {
                foreach (EntityEntry syncableDtoEntry in ChangeTracker.Entries().ToArray())
                {
                    if (syncableDtoEntry.Entity is ISyncableDto syncableDto)
                    {
                        if (syncableDtoEntry.State == EntityState.Deleted && syncableDto.Version != 0)
                        {
                            syncableDto.IsArchived = true;
                            syncableDtoEntry.State = EntityState.Modified;
                        }

                        if (syncableDtoEntry.State == EntityState.Modified || syncableDtoEntry.State == EntityState.Added)
                        {
                            Entry(syncableDto).Property("IsSynced").CurrentValue = false;
                        }
                    }
                }
            }
        }
    }
}
