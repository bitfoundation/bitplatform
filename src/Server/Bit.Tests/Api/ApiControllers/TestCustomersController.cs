using Bit.OData.ODataControllers;
using Bit.Tests.Model.DomainModels;
using Bit.Tests.Model.Dto;
using Microsoft.AspNet.OData;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace Bit.Tests.Api.ApiControllers
{
    public class TestCustomersController : DtoSetController<TestCustomerDto, TestCustomer, Guid>
    {
        public override Task<SingleResult<TestCustomerDto>> Create(TestCustomerDto dto, CancellationToken cancellationToken)
        {
            dto.Name += "#";

            return base.Create(dto, cancellationToken);
        }

        public override Task<SingleResult<TestCustomerDto>> PartialUpdate(Guid key, Delta<TestCustomerDto> modifiedDtoDelta, CancellationToken cancellationToken)
        {
            TestCustomerDto dto = modifiedDtoDelta.GetInstance();
            dto.Name += "#";

            return base.PartialUpdate(key, modifiedDtoDelta, cancellationToken);
        }

        public override Task Delete(Guid key, CancellationToken cancellationToken)
        {
            return base.Delete(key, cancellationToken);
        }
    }
}
