using Bit.Core.Models;
using Swashbuckle.Swagger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.Description;
using System.Web.Http.Filters;

namespace Bit.WebApi.Implementations
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class SwaggerIgnoreAuthorizeAttribute : Attribute
    {

    }

    public class DefaultAuthorizationOperationFilter : IOperationFilter
    {
        public virtual AppEnvironment AppEnvironment { get; internal set; }

        private static Lazy<TypeInfo> AuthorizationFilterAttributeTracer = new Lazy<TypeInfo>(() =>
        {
            return typeof(AuthorizationFilterAttribute).Assembly.GetType("System.Web.Http.Tracing.Tracers.AuthorizationFilterAttributeTracer").GetTypeInfo();
        }, isThreadSafe: true);

        private static Lazy<PropertyInfo> InnerProperty = new Lazy<PropertyInfo>(() =>
        {
            return AuthorizationFilterAttributeTracer.Value.GetProperty("Inner");
        }, isThreadSafe: true);

        public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
        {
            bool hasAllowAnonymous = apiDescription.ActionDescriptor.GetCustomAttributes<AllowAnonymousAttribute>().Any();

            if (hasAllowAnonymous == true)
                return;

            bool hasAuthAttribute = apiDescription.ActionDescriptor.GetFilterPipeline().Any(e =>
            {
                bool isAuthorizeFilter = e.Instance is AuthorizationFilterAttribute;
                if (isAuthorizeFilter)
                {
                    IFilter innerActionFilter = (IFilter)InnerProperty.Value.GetValue(e.Instance);
                    return innerActionFilter.GetType().GetCustomAttribute<SwaggerIgnoreAuthorizeAttribute>() == null;
                }
                return false;
            });

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
