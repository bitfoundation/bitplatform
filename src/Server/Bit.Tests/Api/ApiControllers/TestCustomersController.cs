using Bit.OData.ODataControllers;
using Bit.Tests.Model.DomainModels;
using Bit.Tests.Model.Dto;
using System;

namespace Bit.Tests.Api.ApiControllers
{
    public class TestCustomersController : DtoSetController<TestCustomerDto, TestCustomer, Guid>
    {

    }
}
