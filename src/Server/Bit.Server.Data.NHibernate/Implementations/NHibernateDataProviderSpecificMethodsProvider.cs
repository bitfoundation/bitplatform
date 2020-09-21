using Bit.Data.Contracts;
using Bit.Data.Implementations;
using NHibernate.Linq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Data.NHibernate.Implementations
{
    public class NHibernateDataProviderSpecificMethodsProvider : DefaultDataProviderSpecificMethodsProvider, IDataProviderSpecificMethodsProvider
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

            return source.LongCountAsync(cancellationToken);
        }

        public override bool SupportsConstantParameterization()
        {
            return true;
        }

        public override bool SupportsExpand()
        {
            return true;
        }

        public override bool SupportsQueryable<T>(IQueryable source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source is NhQueryable<T>;
        }

        public override async Task<T[]> ToArrayAsync<T>(IQueryable<T> source, CancellationToken cancellationToken)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return (await source.ToListAsync(cancellationToken).ConfigureAwait(false)).ToArray();
        }
    }
}
