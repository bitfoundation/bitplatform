using Bit.Core.Contracts;
using Bit.Tests.Model.Dto;
using Refit;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace Bit.Tests.Api.ApiControllers
{
    public interface ICustomersService
    {
        [Get("/api/customers/sum/{firstNumber}/{secondNumber}")]
        Task<int> Sum(int firstNumber, int secondNumber, CancellationToken cancellationToken);
    }

    [RoutePrefix("customers")]
    public class SimpleApiController : ApiController, ICustomersService
    {
        [Route("{customerId}/orders")]
        [HttpGet]
        public virtual IHttpActionResult FindOrdersByCustomer(int customerId)
        {
            return Ok(customerId);
        }

        [Route("sum/{firstNumber}/{secondNumber}")]
        [HttpGet]
        public async Task<int> Sum(int firstNumber, int secondNumber, CancellationToken cancellationToken)
        {
            return firstNumber + secondNumber;
        }

        [Route("test")]
        public virtual HttpResponseMessage Test([FromUri]Filter filter)
        {
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpPost]
        [Route("some-action")]
        public virtual TestCustomerDto SomeAction(TestCustomerDto customer)
        {
            return customer;
        }

       public virtual IUserInformationProvider UserInformationProvider { get; set; }

        [HttpGet]
        [Route("get-custom-data")]
        public virtual string GetCustomData()
        {
            return UserInformationProvider.GetBitJwtToken().CustomProps["custom-data"];
        }
    }

    public class Filter
    {
        public string Name { get; set; }
        public int MaxResultCount { get; set; } = 10;
        public int SkipCount { get; set; } = 0;
        public string Sorting { get; set; } = "Name";
    }
}
