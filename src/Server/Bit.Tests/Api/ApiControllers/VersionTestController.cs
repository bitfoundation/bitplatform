using Microsoft.Web.Http;
using System.Web.Http;

namespace Bit.Tests.Api.ApiControllers
{
    [ApiVersion("1.0", Deprecated = true)]
    [ApiVersion("2.0")]
    [RoutePrefix("v{version:apiVersion}/version-test")]
    public class VersionTestController : ApiController
    {
        [HttpGet]
        [Route("get-value")]
        [MapToApiVersion("1.0")]
        public virtual int GetValue_V1()
        {
            return 1;
        }

        [HttpGet]
        [Route("get-value")]
        [MapToApiVersion("2.0")]
        public virtual int GetValue_V2()
        {
            return 2;
        }
    }
}
