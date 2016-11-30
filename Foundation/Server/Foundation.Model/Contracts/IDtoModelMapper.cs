using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Foundation.Model.Contracts
{
    public interface IDtoModelMapper<TDto, TModel>
        where TDto : class, IDto
        where TModel : class, IEntity
    {
        TModel FromDtoToModel(TDto dto, TModel existingModel = null);

        TDto FromModelToDto(TModel model, TDto existingDto = null);

        IQueryable<TDto> FromModelQueryToDtoQuery(IQueryable<TModel> modelQuery);
    }
}
