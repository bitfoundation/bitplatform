using BitChangeSetManager.Model;
using Foundation.Core.Contracts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BitChangeSetManager.DataAccess
{
    public interface IChangeSetRepository : IBitChangeSetManagerRepository<ChangeSet>
    {

    }

    public class ChangeSetRepository : BitChangeSetManagerEfRepository<ChangeSet>, IChangeSetRepository
    {
        private readonly IDateTimeProvider _dateTimeProvider;

        public ChangeSetRepository(IDateTimeProvider dateTimeProvider, BitChangeSetManagerDbContext dbContext)
            : base(dbContext)
        {
            _dateTimeProvider = dateTimeProvider;
        }

        protected override async Task OnAddingAsync(IEnumerable<ChangeSet> entities)
        {
            foreach (ChangeSet changeSet in entities)
            {
                changeSet.CreatedOn = _dateTimeProvider.GetCurrentUtcDateTime();
            }

            await base.OnAddingAsync(entities);
        }
    }
}