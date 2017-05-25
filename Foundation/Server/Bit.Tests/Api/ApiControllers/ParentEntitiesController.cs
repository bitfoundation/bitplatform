using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Foundation.Api.ApiControllers;
using Foundation.Test.Model.DomainModels;
using Microsoft.EntityFrameworkCore;
using Foundation.Api.Exceptions;
using Foundation.Test.Metadata;
using System;
using Foundation.DataAccess.Contracts;

namespace Foundation.Test.Api.ApiControllers
{
    public class ParentEntitiesController : DtoController<ParentEntity>
    {
        private readonly IRepository<ParentEntity> _parentModelsRepository;

        public ParentEntitiesController(IRepository<ParentEntity> parentModelsRepository)
        {
            if (parentModelsRepository == null)
                throw new ArgumentNullException(nameof(parentModelsRepository));

            _parentModelsRepository = parentModelsRepository;
        }

        protected ParentEntitiesController()
        {

        }

        [Get]
        [AllowAnonymous]
        public virtual async Task<IQueryable<ParentEntity>> Get(CancellationToken cancellationToken)
        {
            return await _parentModelsRepository
                .GetAllAsync(cancellationToken);
        }

        [Get]
        public virtual async Task<ParentEntity> Get(long key, CancellationToken cancellationToken)
        {
            ParentEntity parentEntity = await (await _parentModelsRepository
                .GetAllAsync(cancellationToken))
                .FirstOrDefaultAsync(t => t.Id == key, cancellationToken);

            if (parentEntity == null)
                throw new ResourceNotFoundException();

            return parentEntity;
        }

        [Create]
        public virtual async Task<ParentEntity> Create(ParentEntity model, CancellationToken cancellationToken)
        {
            model = await _parentModelsRepository.AddAsync(model, cancellationToken);

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
