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
        [Get("/api/operations/sum/{firstNumber}/{secondNumber}")]
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

        [Route("operations/sum/{firstNumber}/{secondNumber}")]
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
    }

    public class Filter
    {
        public string Name { get; set; }
        public int MaxResultCount { get; set; } = 10;
        public int SkipCount { get; set; } = 0;
        public string Sorting { get; set; } = "Name";
    }
}
