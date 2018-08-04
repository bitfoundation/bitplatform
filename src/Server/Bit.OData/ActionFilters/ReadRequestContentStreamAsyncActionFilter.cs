using Bit.OData.ODataControllers;
using Bit.OData.Serialization;
using Bit.WebApi.Contracts;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace Bit.OData.ActionFilters
{
    public class ReadRequestContentStreamAsyncActionFilter : AuthorizeAttribute, IWebApiConfigurationCustomizer
    {
        public virtual void CustomizeWebApiConfiguration(HttpConfiguration webApiConfiguration)
        {
            webApiConfiguration.Filters.Add(this);
        }

        /// <summary>
        /// <see cref="ExtendedODataDeserializerProvider.GetODataDeserializer(System.Type, HttpRequestMessage)"/>
        /// <see cref="DefaultODataActionCreateUpdateParameterDeserializer.Read(Microsoft.OData.ODataMessageReader, System.Type, Microsoft.AspNet.OData.Formatter.Deserialization.ODataDeserializerContext)"/>
        /// </summary>
        public async override Task OnAuthorizationAsync(HttpActionContext actionContext, CancellationToken cancellationToken)
        {
            HttpActionDescriptor actionDescriptor = actionContext.Request.GetActionDescriptor();

            if (actionDescriptor != null && (actionDescriptor.GetCustomAttributes<ActionAttribute>().Count > 0
                || actionDescriptor.GetCustomAttributes<CreateAttribute>().Count > 0
                || actionDescriptor.GetCustomAttributes<UpdateAttribute>().Count > 0
                || actionDescriptor.GetCustomAttributes<PartialUpdateAttribute>().Count > 0))
            {
                using (StreamReader requestStreamReader = new StreamReader(await actionContext.Request.Content.ReadAsStreamAsync().ConfigureAwait(false)))
                {
                    actionContext.Request.Properties["ContentStreamAsJson"] = await requestStreamReader.ReadToEndAsync().ConfigureAwait(false);
                }
            }

            await base.OnAuthorizationAsync(actionContext, cancellationToken).ConfigureAwait(false);
        }

        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            return true;
        }
    }
}
