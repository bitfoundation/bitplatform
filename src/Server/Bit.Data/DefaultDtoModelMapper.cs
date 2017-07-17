using System;
using System.Linq;
using System.Reflection;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Bit.Model.Contracts;

namespace Bit.Data
{
    public class DefaultDtoModelMapper<TDto, TModel> : IDtoModelMapper<TDto, TModel>
        where TDto : class, IDto
        where TModel : class, IEntity
    {
        private static readonly bool _modelAndDtoAreSame = typeof(TModel).GetTypeInfo() == typeof(TDto).GetTypeInfo();
        private readonly IMapper _mapper;

        public DefaultDtoModelMapper(IMapper mapper)
        {
            if (mapper == null)
                throw new ArgumentNullException(nameof(mapper));

            _mapper = mapper;
        }

#if DEBUG
        protected DefaultDtoModelMapper()
        {
        }
#endif

        public virtual TModel FromDtoToModel(TDto dto, TModel existingModel)
        {
            if (_modelAndDtoAreSame == true)
                return dto as TModel;

            return existingModel == null ? _mapper.Map<TDto, TModel>(dto) : _mapper.Map(dto, existingModel);
        }

        public virtual IQueryable<TDto> FromModelQueryToDtoQuery(IQueryable<TModel> modelQuery)
        {
            if (modelQuery == null)
                throw new ArgumentNullException(nameof(modelQuery));

            if (_modelAndDtoAreSame == true)
                return modelQuery as IQueryable<TDto>;

            return modelQuery.ProjectTo<TDto>(configuration: _mapper.ConfigurationProvider);
        }

        public IQueryable<TDto> FromModelQueryToDtoQuery(IQueryable<TModel> modelQuery, object parameters)
        {
            if (modelQuery == null)
                throw new ArgumentNullException(nameof(modelQuery));

            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            if (_modelAndDtoAreSame == true)
                return modelQuery as IQueryable<TDto>;

            return modelQuery.ProjectTo<TDto>(configuration: _mapper.ConfigurationProvider, parameters: parameters);
        }

        public virtual TDto FromModelToDto(TModel model, TDto existingDto)
        {
            if (_modelAndDtoAreSame == true)
                return model as TDto;

            return existingDto == null ? _mapper.Map<TModel, TDto>(model) : _mapper.Map(model, existingDto);
        }
    }
}
