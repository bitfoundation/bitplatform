using Bit.OData.ODataControllers;
using Bit.Data.Contracts;
using Bit.Tests.Model.DomainModels;
using Bit.Tests.Model.Dto;

namespace Bit.Tests.Api.ApiControllers
{
    public class TestCustomersController : DefaultDtoSetController<TestCustomer, TestCustomerDto>
    {
        protected TestCustomersController()
            : base()
        {

        }

        public TestCustomersController(IEntityWithDefaultGuidKeyRepository<TestCustomer> testCustomersRepository)
            : base(testCustomersRepository)
        {

        }
    }
}
