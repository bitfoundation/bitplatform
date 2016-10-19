using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.OData;
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
        public virtual IQueryable<ParentEntity> Get()
        {
            return _parentModelsRepository
                .GetAll()
                .Include(p => p.ChildEntities)
                .ToList()
                .AsQueryable();
        }

        [Get]
        public virtual async Task<ParentEntity> Get([FromODataUri]long key, CancellationToken cancellationToken)
        {
            return await _parentModelsRepository
                .GetAll()
                .SingleAsync(t => t.Id == key, cancellationToken);
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
        public virtual IQueryable<ParentEntity> GetTestData()
        {
            return new[]
            {
                new ParentEntity { Id = 1, Name = "Test", TestModel = new TestModel { Id = 1, StringProperty = "String1" } }
            }.AsQueryable();
        }
    }
}
