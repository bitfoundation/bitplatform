using System.Linq;

namespace Bit.Model.Contracts
{
    public interface IDtoEntityMapper<TDto, TEntity>
        where TDto : class, IDto
        where TEntity : class, IEntity
    {
        TEntity FromDtoToEntity(TDto dto, TEntity existingEntity = null);

        TDto FromEntityToDto(TEntity entity, TDto existingDto = null);

        IQueryable<TDto> FromEntityQueryToDtoQuery(IQueryable<TEntity> entityQuery);

        IQueryable<TDto> FromEntityQueryToDtoQuery(IQueryable<TEntity> entityQuery, object parameters);
    }
}
