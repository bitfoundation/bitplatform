using Bit.Core.Contracts;
using Bit.Core.Models;
using Swashbuckle.Swagger;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Description;
using System.Web.Http.Filters;

namespace Bit.WebApi.Implementations
{
    public class DefaultAuthorizationOperationFilter : IOperationFilter
    {
        public virtual AppEnvironment AppEnvironment { get; internal set; }

        public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
        {
            bool hasAllowAnonymous = apiDescription.ActionDescriptor.GetCustomAttributes<AllowAnonymousAttribute>().Any();

            if (hasAllowAnonymous == true)
                return;

            bool hasAuthAttribute = apiDescription.ActionDescriptor.GetFilterPipeline().Any(e => e.Instance is AuthorizationFilterAttribute);

            if (hasAuthAttribute == false)
                return;

            if (operation.security == null)
                operation.security = new List<IDictionary<string, IEnumerable<string>>>();

            Dictionary<string, IEnumerable<string>> oAuth2Requirements = new Dictionary<string, IEnumerable<string>>
            {
                { "oauth2", AppEnvironment.Security.Scopes }
            };

            operation.security.Add(oAuth2Requirements);
        }
    }
}
