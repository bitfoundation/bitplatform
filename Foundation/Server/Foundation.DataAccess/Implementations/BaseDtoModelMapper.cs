using AutoMapper;
using AutoMapper.QueryableExtensions;
using Foundation.Model.Contracts;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Foundation.DataAccess.Implementations
{
    public abstract class BaseDtoModelMapper<TDto, TModel, TKey> : IDtoModelMapper<TDto, TModel, TKey>
        where TDto : class, IDto
        where TModel : class, IEntityWithDefaultKey<TKey>
        where TKey : struct
    {
        private static readonly bool _modelAndDtoAreSame = typeof(TModel).GetTypeInfo() == typeof(TDto).GetTypeInfo();
        private readonly IMapper _mapper;

        public BaseDtoModelMapper(IMapper mapper)
        {
            if (mapper == null)
                throw new ArgumentNullException(nameof(mapper));

            _mapper = mapper;
        }

        protected BaseDtoModelMapper()
        {

        }

        public virtual TModel FromDtoToModel(TDto dto)
        {
            if (_modelAndDtoAreSame == true)
                return dto as TModel;

            return _mapper.Map<TDto, TModel>(dto);
        }

        public virtual IQueryable<TDto> FromModelQueryToDtoQuery(IQueryable<TModel> modelQuery)
        {
            if (modelQuery == null)
                throw new ArgumentNullException(nameof(modelQuery));

            if (_modelAndDtoAreSame == true)
                return modelQuery as IQueryable<TDto>;

            return modelQuery.ProjectTo<TDto>(configuration: _mapper.ConfigurationProvider);
        }

        public abstract Task<TDto> GetDtoByKeyFromQueryAsync(IQueryable<TDto> query, TKey key, CancellationToken cancellationToken);

        public abstract TDto GetDtoByKeyFromQuery(IQueryable<TDto> query, TKey key);

        public virtual TDto FromModelToDto(TModel model)
        {
            if (_modelAndDtoAreSame == true)
                return model as TDto;

            return _mapper.Map<TModel, TDto>(model);
        }
    }
}
