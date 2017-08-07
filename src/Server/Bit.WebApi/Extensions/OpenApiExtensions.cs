using Bit.WebApi.Implementations;
using System;
using System.Net.Http;
using System.Threading;
using System.Web.Http;

namespace Swashbuckle.Application
{
    public static class OpenApiExtensions
    {
        /// <summary>
        /// Calls <see cref="SwaggerDocsConfig.DescribeAllEnumsAsStrings(bool)"/>
        /// | Make it compatible with owin branching
        /// | Ignores CancellationToken parameter type
        /// </summary>
        public static SwaggerDocsConfig ApplyDefaultApiConfig(this SwaggerDocsConfig doc, HttpConfiguration webApiConfig)
        {
            doc.DescribeAllEnumsAsStrings();
            doc.RootUrl(req => new Uri(req.RequestUri, req.GetOwinContext().Request.PathBase.Value).ToString());
            doc.OperationFilter<OpenApiIgnoreParameterTypeOperationFilter<CancellationToken>>();

            return doc;
        }
    }
}
