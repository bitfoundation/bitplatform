using Foundation.Api.ApiControllers;
using Foundation.DataAccess.Contracts;
using Foundation.Test.Model.DomainModels;
using Foundation.Test.Model.Dto;

namespace Foundation.Test.Api.ApiControllers
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
