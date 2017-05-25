using Microsoft.AspNetCore.Mvc;

namespace Foundation.AspNetCore.Test.Api.WebApiCoreControllers
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
