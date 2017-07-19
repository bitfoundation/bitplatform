using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Data.Contracts
{
    public interface IDataProviderSpecificMethodsProvider
    {
        bool SupportsQueryable<T>(IQueryable source);

        bool SupportsConstantParameterization();

        Task<List<T>> ToListAsync<T>(IQueryable<T> source, CancellationToken cancellationToken);

        Task<long> LongCountAsync<T>(IQueryable<T> source, CancellationToken cancellationToken);

        Task<T> FirstOrDefaultAsync<T>(IQueryable<T> source, CancellationToken cancellationToken);

        IQueryable<T> Take<T>(IQueryable<T> source, int count);

        IQueryable<T> Skip<T>(IQueryable<T> source, int count);

        IQueryable<T> ApplyWhereByKeys<T>(IQueryable<T> source, params object[] keys);
    }
}
