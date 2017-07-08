using Bit.OData.ODataControllers;
using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Dto;
using BitChangeSetManager.Model;

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