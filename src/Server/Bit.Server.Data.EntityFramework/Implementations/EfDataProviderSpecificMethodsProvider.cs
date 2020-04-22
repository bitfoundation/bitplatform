using Bit.Data.Contracts;
using Bit.Data.Implementations;
using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Data.EntityFramework.Implementations
{
    public class EfDataProviderSpecificMethodsProvider : DefaultDataProviderSpecificMethodsProvider, IDataProviderSpecificMethodsProvider
    {
        public override Task<T> FirstOrDefaultAsync<T>(IQueryable<T> source, CancellationToken cancellationToken)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.FirstOrDefaultAsync(cancellationToken);
        }

        public override Task<long> LongCountAsync<T>(IQueryable<T> source, CancellationToken cancellationToken)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source
                .LongCountAsync(cancellationToken);
        }

        public override IQueryable<T> Skip<T>(IQueryable<T> source, int count)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.Skip(() => count);
        }

        public override bool SupportsConstantParameterization()
        {
            return true;
        }

        public override bool SupportsQueryable<T>(IQueryable source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source is DbQuery<T>;
        }

        public override IQueryable<T> Take<T>(IQueryable<T> source, int count)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.Take(() => count);
        }

        public override Task<T[]> ToArrayAsync<T>(IQueryable<T> source, CancellationToken cancellationToken)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source
                .ToArrayAsync(cancellationToken);
        }

        public override IQueryable<T> ApplyWhereByKeys<T>(IQueryable<T> source, params object[] keys)
        {
            // TODO: If T is IEntity, then we've to get key columns from entity framework's workspace metadata

            return base.ApplyWhereByKeys(source, keys);
        }
    }
}
