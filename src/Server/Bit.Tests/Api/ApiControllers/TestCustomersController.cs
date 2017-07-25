using Bit.Data.Contracts;
using Bit.OData.ODataControllers;
using Bit.Tests.Model.DomainModels;
using Bit.Tests.Model.Dto;

namespace Bit.Tests.Api.ApiControllers
{
    public class TestCustomersController : DefaultDtoSetController<TestCustomerDto, TestCustomer>
    {
        public TestCustomersController(IRepository<TestCustomer> testCustomersRepository)
            : base(testCustomersRepository)
        {

        }

#if DEBUG
        protected TestCustomersController()
        {

        }
#endif
    }
}
