using Foundation.Api.ApiControllers;
using Foundation.DataAccess.Contracts;
using Foundation.Test.Model.DomainModels;
using Foundation.Test.Model.Dto;
using System;

namespace Foundation.Test.Api.ApiControllers
{
    public class TestCustomersController : DtoSetController<TestCustomer, TestCustomerDto, Guid>
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
