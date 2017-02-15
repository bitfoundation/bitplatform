using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Foundation.DataAccess.Contracts
{
    public interface IDataProviderSpecificMethodsProvider
    {
        bool SupportsQueryable<T>(IQueryable source);

        Task<List<T>> ToListAsync<T>(IQueryable<T> source, CancellationToken cancellationToken);

        Task<long> LongCountAsync<T>(IQueryable<T> source, CancellationToken cancellationToken);

        Task<T> FirstOrDefaultAsync<T>(IQueryable<T> source, CancellationToken cancellationToken);
    }
}
