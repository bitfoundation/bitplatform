using System.Net;
using System.Net.Http;
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
                if (((ObjectContent)(actionExecutedContext.Response.Content)).Value == null)
                {
                    if (actionExecutedContext.Response.IsSuccessStatusCode)
                        actionExecutedContext.Response.StatusCode = HttpStatusCode.NoContent;
                    actionExecutedContext.Response.Content = null;
                }
                else
                    base.OnActionExecuted(actionExecutedContext);
            }
        }
    }
}
