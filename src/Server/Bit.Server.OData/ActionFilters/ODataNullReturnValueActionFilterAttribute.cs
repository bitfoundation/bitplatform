using Microsoft.AspNet.OData.Extensions;
using System;
using System.Collections;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;

namespace Bit.OData.ActionFilters
{
    public class ODataNullReturnValueActionFilterAttribute : ActionFilterAttribute
    {
        public override Task OnActionExecutedAsync(HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken)
        {
            if (actionExecutedContext == null)
                throw new ArgumentNullException(nameof(actionExecutedContext));

            if (actionExecutedContext.Response?.Content is ObjectContent objContent &&
               actionExecutedContext.Response.IsSuccessStatusCode == true)
            {
                if (objContent.Value != null)
                    return Task.CompletedTask;

                TypeInfo actionReturnType = objContent.ObjectType.GetTypeInfo();

                bool isEnumerable = typeof(string) != actionReturnType && typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(actionReturnType);

                if (isEnumerable == true)
                {
                    TypeInfo queryElementType = actionReturnType.HasElementType ? (actionReturnType.GetElementType()!).GetTypeInfo() : actionReturnType.GetGenericArguments().First().GetTypeInfo();
                    objContent.Value = Array.CreateInstance(queryElementType, 0);
                }
                else
                {
                    string edmTypeFullPath = $"{actionExecutedContext.Request.GetReaderSettings().BaseUri}$metadata#Edm.Null";
                    actionExecutedContext.Response.Content = new StringContent($"{{\"@odata.context\":\"{edmTypeFullPath}\",\"@odata.null\":true}}", Encoding.UTF8, "application/json");
                    actionExecutedContext.Response.Headers.Add("OData-Version", "4.0");
                    actionExecutedContext.Response.Content.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("odata.metadata", "minimal"));
                }
            }

            return Task.CompletedTask;
        }
    }
}
