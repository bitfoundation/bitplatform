using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Bit.OData.ODataControllers;
using Bit.Data.Contracts;
using Bit.Owin.Exceptions;
using Bit.Tests.Model.DomainModels;
using Bit.Tests.Owin.Metadata;
using Microsoft.EntityFrameworkCore;

namespace Bit.Tests.Api.ApiControllers
{
    public class ParentEntitiesController : DtoController<ParentEntity>
    {
        private readonly IRepository<ParentEntity> _parentEntitiesRepository;

        public ParentEntitiesController(IRepository<ParentEntity> parentModelsRepository)
        {
            if (parentModelsRepository == null)
                throw new ArgumentNullException(nameof(parentModelsRepository));

            _parentEntitiesRepository = parentModelsRepository;
        }

        protected ParentEntitiesController()
        {

        }

        [Get]
        [AllowAnonymous]
        public virtual async Task<IQueryable<ParentEntity>> Get(CancellationToken cancellationToken)
        {
            return await _parentEntitiesRepository
                .GetAllAsync(cancellationToken);
        }

        [Get]
        public virtual async Task<ParentEntity> Get(long key, CancellationToken cancellationToken)
        {
            ParentEntity parentEntity = await (await _parentEntitiesRepository
                .GetAllAsync(cancellationToken))
                .FirstOrDefaultAsync(t => t.Id == key, cancellationToken);

            if (parentEntity == null)
                throw new ResourceNotFoundException();

            return parentEntity;
        }

        [Create]
        public virtual async Task<ParentEntity> Create(ParentEntity model, CancellationToken cancellationToken)
        {
            model = await _parentEntitiesRepository.AddAsync(model, cancellationToken);

            if (model.Name == "KnownError")
                throw new DomainLogicException(TestMetadataBuilder.SomeError);
            else if (model.Name == "UnknowError")
                throw new InvalidOperationException("Some unknown error");

            model.Id = 999;

            return model;
        }

        [Function]
        public virtual ParentEntity[] GetTestData()
        {
            return new[]
            {
                new ParentEntity { Id = 1, Name = "Test", TestModel = new TestModel { Id = 1, StringProperty = "String1" } }
            };
        }
    }
}
