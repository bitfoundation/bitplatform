using Microsoft.AspNetCore.Mvc;

namespace Bit.Tests.Api.ApiControllers
{
    [Controller]
    public class PeopleController
    {
        [HttpGet]
        public virtual JsonResult GetData()
        {
            return new JsonResult(new { FirstName = "Test" });
        }
    }
}
