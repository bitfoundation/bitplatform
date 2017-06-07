using System.Reflection;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.OData;

namespace Bit.Api.Middlewares.WebApi.OData.ActionFilters
{
    public class DefaultODataAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            if (actionContext.ControllerContext.ControllerDescriptor.ControllerType.GetTypeInfo() == typeof(MetadataController).GetTypeInfo())
                return true;

            return base.IsAuthorized(actionContext);
        }
    }
}
