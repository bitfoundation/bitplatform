using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.OData;
using Foundation.Api.ApiControllers;
using Foundation.Test.Model.DomainModels;
using Microsoft.EntityFrameworkCore;
using System;
using Foundation.DataAccess.Contracts;
using Foundation.Api.Exceptions;

namespace Foundation.Test.Api.ApiControllers
{
    public class ChildEntitiesController : DtoController<ChildEntity>
    {
        private readonly IRepository<ChildEntity> _testRepository;

        public ChildEntitiesController(IRepository<ChildEntity> testRepository)
        {
            if (testRepository == null)
                throw new ArgumentNullException(nameof(testRepository));

            _testRepository = testRepository;
        }

        protected ChildEntitiesController()
        {

        }

        [Get]
        [AllowAnonymous]
        public virtual IQueryable<ChildEntity> Get()
        {
            return _testRepository
                .GetAll();
        }

        [Get]
        public virtual async Task<ChildEntity> Get([FromODataUri]long key, CancellationToken cancellationToken)
        {
            ChildEntity childEntity = await _testRepository
                .GetAll()
                .FirstOrDefaultAsync(t => t.Id == key, cancellationToken);

            if (childEntity == null)
                throw new ResourceNotFoundException();

            return childEntity;
        }

        [Create]
        public virtual async Task<ChildEntity> Create(ChildEntity model, CancellationToken cancellationToken)
        {
            model = await _testRepository.AddAsync(model, cancellationToken);

            return model;
        }
    }
}
