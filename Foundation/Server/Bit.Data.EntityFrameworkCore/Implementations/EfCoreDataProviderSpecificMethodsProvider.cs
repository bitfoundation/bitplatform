using Foundation.DataAccess.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace Foundation.DataAccess.Implementations.EntityFrameworkCore
{
    public class EfCoreDataProviderSpecificMethodsProvider : IDataProviderSpecificMethodsProvider
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

        public virtual IQueryable<T> Skip<T>(IQueryable<T> source, int count)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.Skip(count);
        }

        public virtual bool SupportsConstantParameterization()
        {
            return true;
        }

        public bool SupportsQueryable<T>(IQueryable source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source is EntityQueryable<T>;
        }

        public virtual IQueryable<T> Take<T>(IQueryable<T> source, int count)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.Take(count);
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
