using BitChangeSetManager.Model;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bit.Core.Contracts;

namespace BitChangeSetManager.DataAccess
{
    public interface IChangeSetRepository : IBitChangeSetManagerRepository<ChangeSet>
    {

    }

    public class ChangeSetRepository : BitChangeSetManagerEfRepository<ChangeSet>, IChangeSetRepository
    {
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly BitChangeSetManagerDbContext _dbContext;

        public ChangeSetRepository(IDateTimeProvider dateTimeProvider, BitChangeSetManagerDbContext dbContext)
            : base(dbContext)
        {
            _dateTimeProvider = dateTimeProvider;
            _dbContext = dbContext;
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
            _dbContext.ChangeTracker.DetectChanges();

            foreach (DbEntityEntry<ChangeSet> changeSet in _dbContext.ChangeTracker.Entries<ChangeSet>().Where(e => e.State == EntityState.Added))
            {
                changeSet.Entity.CreatedOn = _dateTimeProvider.GetCurrentUtcDateTime();
            }
        }
    }
}