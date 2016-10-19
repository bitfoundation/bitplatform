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
                .GetAll()
                .Include(c => c.ParentEntity)
                .ToList()
                .AsQueryable();
        }

        [Get]
        public virtual async Task<ChildEntity> Get([FromODataUri]long key, CancellationToken cancellationToken)
        {
            return await _testRepository
                .GetAll()
                .SingleAsync(t => t.Id == key, cancellationToken);
        }

        [Create]
        public virtual async Task<ChildEntity> Create(ChildEntity model, CancellationToken cancellationToken)
        {
            model = await _testRepository.AddAsync(model, cancellationToken);

            return model;
        }
    }
}
