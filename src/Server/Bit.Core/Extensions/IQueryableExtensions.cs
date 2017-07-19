using System.Linq.Expressions;

namespace System.Linq
{
    public static class IQueryableExtensions
    {
        public static IQueryable<T> WhereIf<T>(this IQueryable<T> source, bool applyPredicate, Expression<Func<T, bool>> predicate)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (applyPredicate == true)
                source = source.Where(predicate);

            return source;
        }
    }
}
