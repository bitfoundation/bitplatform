using Bit.Data.Contracts;
using Bit.Model.Implementations;
using System;
using System.Linq;
using System.Linq.Dynamic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Data.Implementations
{
    internal class ExtendedDataProviderSpecificMethodsProvider : DefaultDataProviderSpecificMethodsProvider
    {
        public override bool SupportsQueryable<T>(IQueryable source)
        {
            return true;
        }
    }

    public abstract class DefaultDataProviderSpecificMethodsProvider : IDataProviderSpecificMethodsProvider
    {
        private static IDataProviderSpecificMethodsProvider _current;

        public static IDataProviderSpecificMethodsProvider Current
        {
            get => _current ?? (_current = new ExtendedDataProviderSpecificMethodsProvider());
            set => _current = value;
        }

        public virtual IQueryable<T> ApplyWhereByKeys<T>(IQueryable<T> source, params object[] keys)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            PropertyInfo[] keyProps = DtoMetadataWorkspace.Current.GetKeyColums(typeof(T).GetTypeInfo());

            foreach (var keyValue in keyProps.Zip(keys, (key, value) => new { key, value }))
            {
                source = source.Where($"{keyValue.key.Name} == @0", keyValue.value);
            }

            return source;
        }

        public virtual Task<T> FirstOrDefaultAsync<T>(IQueryable<T> source, CancellationToken cancellationToken)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return Task.FromResult(source.FirstOrDefault());
        }

        public virtual Task<long> LongCountAsync<T>(IQueryable<T> source, CancellationToken cancellationToken)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return Task.FromResult(source.LongCount());
        }

        public virtual IQueryable<T> Skip<T>(IQueryable<T> source, int count)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.Skip(count);
        }

        public virtual bool SupportsConstantParameterization()
        {
            return false;
        }

        public abstract bool SupportsQueryable<T>(IQueryable source);

        public virtual IQueryable<T> Take<T>(IQueryable<T> source, int count)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.Take(count);
        }

        public virtual Task<T[]> ToArrayAsync<T>(IQueryable<T> source, CancellationToken cancellationToken)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return Task.FromResult(source.ToArray());
        }
    }
}
