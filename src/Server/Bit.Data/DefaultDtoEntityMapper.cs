using AutoMapper;
using AutoMapper.QueryableExtensions;
using Bit.Model.Contracts;
using System;
using System.Linq;
using System.Reflection;

namespace Bit.Data
{
    public class DefaultDtoEntityMapper<TDto, TEntity> : IDtoEntityMapper<TDto, TEntity>
        where TDto : class, IDto
        where TEntity : class, IEntity
    {
        private static readonly bool _entityTypeAndDtoTypeAreEqual = typeof(TEntity).GetTypeInfo() == typeof(TDto).GetTypeInfo();

        public virtual IMapper Mapper { get; set; }

        public virtual TEntity FromDtoToEntity(TDto dto, TEntity existingEntity)
        {
            if (_entityTypeAndDtoTypeAreEqual)
                return dto as TEntity;

            return existingEntity == null ? Mapper.Map<TDto, TEntity>(dto) : Mapper.Map(dto, existingEntity);
        }

        public virtual IQueryable<TDto> FromEntityQueryToDtoQuery(IQueryable<TEntity> entityQuery)
        {
            if (entityQuery == null)
                throw new ArgumentNullException(nameof(entityQuery));

            if (_entityTypeAndDtoTypeAreEqual)
                return (IQueryable<TDto>)entityQuery;

            return entityQuery.ProjectTo<TDto>(configuration: Mapper.ConfigurationProvider);
        }

        public IQueryable<TDto> FromEntityQueryToDtoQuery(IQueryable<TEntity> entityQuery, object parameters)
        {
            if (entityQuery == null)
                throw new ArgumentNullException(nameof(entityQuery));

            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            if (_entityTypeAndDtoTypeAreEqual)
                return (IQueryable<TDto>)entityQuery;

            return entityQuery.ProjectTo<TDto>(configuration: Mapper.ConfigurationProvider, parameters: parameters);
        }

        public virtual TDto FromEntityToDto(TEntity entity, TDto existingDto)
        {
            if (_entityTypeAndDtoTypeAreEqual)
                return entity as TDto;

            return existingDto == null ? Mapper.Map<TEntity, TDto>(entity) : Mapper.Map(entity, existingDto);
        }
    }
}
