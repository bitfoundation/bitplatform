using Bit.Data.Contracts;
using Bit.OData.ODataControllers;
using Bit.Owin.Exceptions;
using Bit.Tests.Model.DomainModels;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace Bit.Tests.Api.ApiControllers
{
    public class ChildEntitiesController : DtoController<ChildEntity>
    {
        public virtual IRepository<ChildEntity> TestRepository { get; set; }

        [Get]
        [AllowAnonymous]
        public virtual Task<IQueryable<ChildEntity>> Get(CancellationToken cancellationToken)
        {
            return TestRepository.GetAllAsync(cancellationToken);
        }

        [Get]
        public virtual async Task<ChildEntity> Get(long key, CancellationToken cancellationToken)
        {
            ChildEntity childEntity = await (await TestRepository
                .GetAllAsync(cancellationToken))
                .FirstOrDefaultAsync(t => t.Id == key, cancellationToken);

            if (childEntity == null)
                throw new ResourceNotFoundException();

            return childEntity;
        }

        [Create]
        public virtual async Task<ChildEntity> Create(ChildEntity model, CancellationToken cancellationToken)
        {
            model = await TestRepository.AddAsync(model, cancellationToken);

            return model;
        }
    }
}
