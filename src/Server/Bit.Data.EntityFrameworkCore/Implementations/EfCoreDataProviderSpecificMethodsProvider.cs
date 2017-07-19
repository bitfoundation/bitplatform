using Bit.Data.Contracts;
using Bit.Data.Implementations;
using Bit.Model.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Data.EntityFrameworkCore.Implementations
{
    public class EfCoreDataProviderSpecificMethodsProvider : DefaultDataProviderSpecificMethodsProvider, IDataProviderSpecificMethodsProvider
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

        public override bool SupportsConstantParameterization()
        {
            return true;
        }

        public override bool SupportsQueryable<T>(IQueryable source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source is EntityQueryable<T>;
        }

        public override Task<List<T>> ToListAsync<T>(IQueryable<T> source, CancellationToken cancellationToken)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source
                .ToListAsync(cancellationToken);
        }

        public override IQueryable<T> ApplyWhereByKeys<T>(IQueryable<T> source, params object[] keys)
        {
            // TODO: If T is IEntity, then we've to get key columns from entity framework's workspace metadata

            return base.ApplyWhereByKeys(source, keys);
        }
    }
}
