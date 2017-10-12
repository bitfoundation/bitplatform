using BitChangeSetManager.Model;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Data.Contracts;

namespace BitChangeSetManager.DataAccess
{
    public interface IChangeSetRepository : IRepository<ChangeSet>
    {

    }

    public class ChangeSetRepository : BitChangeSetManagerEfRepository<ChangeSet>, IChangeSetRepository
    {
        private readonly IDateTimeProvider _dateTimeProvider;

        public ChangeSetRepository(IDateTimeProvider dateTimeProvider)
        {
            _dateTimeProvider = dateTimeProvider;
        }

        public override void SaveChanges()
        {
            SetCreatedOnValue();

            base.SaveChanges();
        }

        public override async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SetCreatedOnValue();

            await base.SaveChangesAsync(cancellationToken);
        }

        private void SetCreatedOnValue()
        {
            DbContext.ChangeTracker.DetectChanges();

            foreach (DbEntityEntry<ChangeSet> changeSet in DbContext.ChangeTracker.Entries<ChangeSet>().Where(e => e.State == EntityState.Added))
            {
                changeSet.Entity.CreatedOn = _dateTimeProvider.GetCurrentUtcDateTime();
            }
        }
    }
}