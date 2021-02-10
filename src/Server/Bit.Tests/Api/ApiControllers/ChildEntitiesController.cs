using Bit.Data.Contracts;
using Bit.OData.ODataControllers;
using Bit.Tests.Model.DomainModels;
using Microsoft.AspNet.OData;
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
        public virtual async Task<IQueryable<ChildEntity>> Get(CancellationToken cancellationToken)
        {
            return await TestRepository
                .GetAllAsync(cancellationToken);
        }

        [Get]
        public virtual async Task<SingleResult<ChildEntity>> Get(long key, CancellationToken cancellationToken)
        {
            return SingleResult((await TestRepository
                .GetAllAsync(cancellationToken))
                .Where(t => t.Id == key));
        }

        [Create]
        public virtual async Task<SingleResult<ChildEntity>> Create(ChildEntity model, CancellationToken cancellationToken)
        {
            model = await TestRepository.AddAsync(model, cancellationToken);

            return SingleResult(model);
        }

        [Update]
        public virtual async Task<SingleResult<ChildEntity>> Update(long key, ChildEntity model, CancellationToken cancellationToken)
        {
            model.Name += "?";

            model = await TestRepository.UpdateAsync(model, cancellationToken);

            return SingleResult(model);
        }

        [PartialUpdate]
        public virtual async Task<SingleResult<ChildEntity>> PartialUpdate(long key, Delta<ChildEntity> modelDelta, CancellationToken cancellationToken)
        {
            var model = modelDelta.GetInstance();

            model.Name += "?";

            model = await TestRepository.UpdateAsync(model, cancellationToken);

            return SingleResult(model);
        }

        [Delete]
        public virtual async Task Delete(long key, CancellationToken cancellationToken)
        {
            await TestRepository.DeleteAsync(await TestRepository.GetByIdAsync(cancellationToken, key), cancellationToken);
        }
    }
}
