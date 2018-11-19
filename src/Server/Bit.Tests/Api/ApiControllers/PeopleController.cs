using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bit.Tests.Api.ApiControllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class PeopleController
    {
        [HttpGet]
        [Authorize]
        public virtual JsonResult GetData()
        {
            return new JsonResult(new { FirstName = "Test" });
        }

        [HttpGet]
        [AllowAnonymous]
        public virtual JsonResult GetData2()
        {
            return new JsonResult(new { FirstName = "Test" });
        }
    }
}
