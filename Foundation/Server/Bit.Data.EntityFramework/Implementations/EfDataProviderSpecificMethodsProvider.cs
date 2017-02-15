using Foundation.DataAccess.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;

namespace Bit.Data.EntityFramework.Implementations
{
    public class EfDataProviderSpecificMethodsProvider : IDataProviderSpecificMethodsProvider
    {
        public Task<T> FirstOrDefaultAsync<T>(IQueryable<T> source, CancellationToken cancellationToken)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.FirstOrDefaultAsync(cancellationToken);
        }

        public virtual Task<long> LongCountAsync<T>(IQueryable<T> source, CancellationToken cancellationToken)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source
                .LongCountAsync(cancellationToken);
        }

        public bool SupportsQueryable<T>(IQueryable source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source is DbQuery<T>;
        }

        public virtual Task<List<T>> ToListAsync<T>(IQueryable<T> source, CancellationToken cancellationToken)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source
                .ToListAsync(cancellationToken);
        }
    }
}
