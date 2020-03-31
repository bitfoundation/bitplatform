using Bit.OData.ODataControllers;
using Bit.OData.Serialization;
using Bit.WebApi.Contracts;
using Bit.WebApi.Implementations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace Bit.OData.ActionFilters
{
    [SwaggerIgnoreAuthorizeAttribute] // It's just for async reading of request body which is possible in AuthorizeActionFilter. Following codes will gets executed before OData DeSerializers.
    public class ReadRequestContentStreamAsyncActionFilterAttribute : AuthorizeAttribute, IWebApiConfigurationCustomizer
    {
        public virtual void CustomizeWebApiConfiguration(HttpConfiguration webApiConfiguration)
        {
            webApiConfiguration.Filters.Add(this);
        }

        /// <summary>
        /// <see cref="ExtendedODataDeserializerProvider.GetODataDeserializer(System.Type, HttpRequestMessage)"/>
        /// <see cref="DefaultODataActionCreateUpdateParameterDeserializer.Read(Microsoft.OData.ODataMessageReader, System.Type, Microsoft.AspNet.OData.Formatter.Deserialization.ODataDeserializerContext)"/>
        /// </summary>
        public override async Task OnAuthorizationAsync(HttpActionContext actionContext, CancellationToken cancellationToken)
        {
            HttpActionDescriptor actionDescriptor = actionContext.Request.GetActionDescriptor();

            HttpContentHeaders requestContentHeaders = actionContext?.Request?.Content?.Headers;

            if (requestContentHeaders != null)
            {
                bool contentLengthHasValue = requestContentHeaders.ContentLength.HasValue;
                bool contentTypeIsJson = requestContentHeaders.ContentType?.MediaType?.Contains("json") == true; // https://github.com/aspnet/AspNetWebStack/issues/232

                if (((contentLengthHasValue && requestContentHeaders.ContentLength > 0) || (!contentLengthHasValue && contentTypeIsJson)) && (actionDescriptor.GetCustomAttributes<ActionAttribute>().Any() ||
                    actionDescriptor.GetCustomAttributes<CreateAttribute>().Any() ||
                    actionDescriptor.GetCustomAttributes<UpdateAttribute>().Any() ||
                    actionDescriptor.GetCustomAttributes<PartialUpdateAttribute>().Any()))
                {
#if DotNet
                    using (Stream responseContent = await actionContext.Request.Content.ReadAsStreamAsync().ConfigureAwait(false))
#else
                    await using (Stream responseContent = await actionContext.Request.Content.ReadAsStreamAsync().ConfigureAwait(false))
#endif
                    using (StreamReader requestStreamReader = new StreamReader(responseContent))
                    {
                        using (JsonReader jsonReader = new JsonTextReader(requestStreamReader))
                        {
                            JToken contentStreamAsJson = await JToken.LoadAsync(jsonReader, cancellationToken).ConfigureAwait(false);

                            if (contentStreamAsJson.First is JProperty prop && actionDescriptor.GetParameters().Any(p => p.ParameterName == prop.Name))
                            {
                                contentStreamAsJson = contentStreamAsJson[prop.Name];
                            }

                            actionContext.Request.Properties["ContentStreamAsJson"] = contentStreamAsJson;
                        }
                    }
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
