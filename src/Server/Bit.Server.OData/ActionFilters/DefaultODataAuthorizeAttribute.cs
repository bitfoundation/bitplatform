using Microsoft.AspNet.OData;
using System;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace Bit.OData.ActionFilters
{
    public class DefaultODataAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            if (actionContext == null)
                throw new ArgumentNullException(nameof(actionContext));

            if (actionContext.ControllerContext.ControllerDescriptor.ControllerType.GetTypeInfo() == typeof(MetadataController).GetTypeInfo())
                return true;

            return base.IsAuthorized(actionContext);
        }
    }
}
