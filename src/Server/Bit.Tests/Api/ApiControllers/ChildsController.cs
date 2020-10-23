using Bit.Core.Exceptions;
using Bit.OData.ODataControllers;
using Bit.Tests.Model.DomainModels;
using Microsoft.AspNet.OData;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace Bit.Tests.Api.ApiControllers
{
    public class ChildsController : DtoController<ChildEntity>
    {
        [Get]
        public virtual async Task<IQueryable<ChildEntity>> GetAll(CancellationToken cancellationToken)
        {
            return new[]
            {
                new ChildEntity { Id = 1, Name = "A", ParentEntityId = 1, Version = 1 },
                new ChildEntity { Id = 2, Name = "B", ParentEntityId = 2, Version = 2 }
            }.AsQueryable();
        }

        [PartialUpdate]
        public virtual async Task<SingleResult<ChildEntity>> PartialUpdate(long key, Delta<ChildEntity> modifiedDtoDelta, CancellationToken cancellationToken)
        {
            var e = modifiedDtoDelta.GetInstance();

            if (e.Name == "Error")
                throw new DomainLogicException("TestErrorMessage");

            e.Name += "?";

            return SingleResult(e);
        }
    }
}
