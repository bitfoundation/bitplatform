using Refit;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace Bit.Tests.Api.ApiControllers
{
    public interface ICustomersService
    {
        [Get("/api/customers/operations/sum/{firstNumber}/{secondNumber}")]
        Task<int> Sum(int firstNumber, int secondNumber, CancellationToken cancellationToken);
    }

    public class SimpleApiController : ApiController, ICustomersService
    {
        [Route("customers/{customerId}/orders")]
        [HttpGet]
        public virtual IHttpActionResult FindOrdersByCustomer(int customerId)
        {
            return Ok();
        }

        [Route("customers/operations/sum/{firstNumber}/{secondNumber}")]
        [HttpGet]
        public async Task<int> Sum(int firstNumber, int secondNumber, CancellationToken cancellationToken)
        {
            return firstNumber + secondNumber;
        }
    }
}
