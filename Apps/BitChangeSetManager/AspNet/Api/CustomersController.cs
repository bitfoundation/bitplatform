using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Dto;
using BitChangeSetManager.Model;
using System;

namespace BitChangeSetManager.Api
{
    public class CustomersController : BitChangeSetManagerDtoSetController<CustomerDto, Customer, Guid>
    {
    }
}