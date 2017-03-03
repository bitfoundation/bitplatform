using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Dto;
using BitChangeSetManager.Model;
using Foundation.Api.ApiControllers;

namespace BitChangeSetManager.Api
{
    public class CustomersController : DefaultDtoSetController<Customer, CustomerDto>
    {
        public CustomersController(IBitChangeSetManagerRepository<Customer> customersRepository)
            : base(customersRepository)
        {

        }
    }
}