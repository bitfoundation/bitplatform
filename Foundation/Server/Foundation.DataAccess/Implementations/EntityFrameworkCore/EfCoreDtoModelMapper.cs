using Foundation.Model.Contracts;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace Foundation.DataAccess.Implementations.EntityFrameworkCore
{
    public class EfCoreDtoModelMapper<TDto, TModel, TKey> : BaseDtoModelMapper<TDto, TModel, TKey>
        where TDto : class, IDto
        where TModel : class, IEntityWithDefaultKey<TKey>
        where TKey : struct
    {
        public EfCoreDtoModelMapper(IMapper mapper)
            : base(mapper)
        {

        }

        protected EfCoreDtoModelMapper()
        {

        }

        public override TDto GetDtoByKeyFromQuery(IQueryable<TDto> query, TKey key)
        {
            return query
                .Where($@"{nameof(IEntityWithDefaultKey<TKey>.Id)}.ToString()=""{key.ToString()}""")
                .Single();
        }

        public override async Task<TDto> GetDtoByKeyFromQueryAsync(IQueryable<TDto> query, TKey key, CancellationToken cancellationToken)
        {
            return await query
                .Where($@"{nameof(IEntityWithDefaultKey<TKey>.Id)}.ToString()=""{key.ToString()}""")
                .SingleAsync(cancellationToken);
        }
    }
}
