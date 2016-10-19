using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Foundation.AspNetCore.Test.Api.WebApiCoreControllers
{
    public class PeopleController : Controller
    {
        [HttpGet]
        public virtual ActionResult GetData()
        {
            return Json(new { FirstName = "Test" });
        }
    }
}
