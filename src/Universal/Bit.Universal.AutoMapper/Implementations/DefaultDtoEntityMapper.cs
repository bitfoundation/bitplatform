using AutoMapper;
using AutoMapper.QueryableExtensions;
using Bit.Model.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Bit.Model.Implementations
{
    public class DefaultDtoEntityMapper<TDto, TEntity> : IDtoEntityMapper<TDto, TEntity>
        where TDto : class
        where TEntity : class, IEntity
    {
        private static readonly bool _entityTypeAndDtoTypeAreEqual = typeof(TEntity).GetTypeInfo() == typeof(TDto).GetTypeInfo();

        public IMapper Mapper { get; set; } = default!;

        public virtual TEntity FromDtoToEntity(TDto dto, TEntity? existingEntity)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (_entityTypeAndDtoTypeAreEqual == true)
                return (dto as TEntity)!;

            return existingEntity == null ? Mapper.Map<TDto, TEntity>(dto) : Mapper.Map(dto, existingEntity);
        }

        public IQueryable<TDto> FromEntityQueryToDtoQuery(IQueryable<TEntity> entityQuery, object? parameters = null, string[]? membersToExpand = null)
        {
            if (entityQuery == null)
                throw new ArgumentNullException(nameof(entityQuery));

            if (_entityTypeAndDtoTypeAreEqual == true)
                return (IQueryable<TDto>)entityQuery;

            Dictionary<string, object?> @params = new Dictionary<string, object?> { };

            if (parameters != null)
            {
                foreach (PropertyInfo prop in parameters.GetType().GetProperties())
                {
                    @params.Add(prop.Name, prop.GetValue(parameters));
                }
            }

            return entityQuery.ProjectTo<TDto>(configuration: Mapper.ConfigurationProvider, parameters: @params, membersToExpand: membersToExpand ?? Array.Empty<string>());
        }

        public virtual TDto FromEntityToDto(TEntity entity, TDto? existingDto)
        {
            if (_entityTypeAndDtoTypeAreEqual == true)
                return (entity as TDto)!;

            return existingDto == null ? Mapper.Map<TEntity, TDto>(entity) : Mapper.Map(entity, existingDto);
        }
    }
}
