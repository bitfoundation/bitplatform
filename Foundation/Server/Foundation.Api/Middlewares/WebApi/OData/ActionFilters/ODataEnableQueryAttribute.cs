using System.Collections;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Web.Http.Filters;
using System.Web.OData;

namespace Foundation.Api.Middlewares.WebApi.OData.ActionFilters
{
    public class ODataEnableQueryAttribute : EnableQueryAttribute
    {
        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            if (actionExecutedContext?.Response?.Content is ObjectContent)
            {
                ObjectContent objContent = ((ObjectContent)(actionExecutedContext.Response.Content));

                if (objContent.Value == null)
                {
                    if (actionExecutedContext.Response.IsSuccessStatusCode)
                        actionExecutedContext.Response.StatusCode = HttpStatusCode.NoContent;
                    actionExecutedContext.Response.Content = null;
                }
                else
                {
                    TypeInfo type = objContent.Value.GetType().GetTypeInfo();
                    if (typeof(string) != type && typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(type))
                        base.OnActionExecuted(actionExecutedContext);
                }
            }
        }
    }
}
