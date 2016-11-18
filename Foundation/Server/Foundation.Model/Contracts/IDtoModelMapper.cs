using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Foundation.Model.Contracts
{
    public interface IDtoModelMapper<TDto, TModel, TKey>
        where TDto : class, IDto
        where TModel : class, IEntityWithDefaultKey<TKey>
        where TKey : struct
    {
        TModel FromDtoToModel(TDto dto, TModel existingModel = null);

        TDto FromModelToDto(TModel model);

        IQueryable<TDto> FromModelQueryToDtoQuery(IQueryable<TModel> modelQuery);

        Task<TDto> GetDtoByKeyFromQueryAsync(IQueryable<TDto> query, TKey key, CancellationToken cancellationToken);

        TDto GetDtoByKeyFromQuery(IQueryable<TDto> query, TKey key);
    }
}
