using Swashbuckle.Swagger;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;

namespace Bit.WebApi.Implementations
{
    public class AddExpandAndSelectToNonGetOperationFilter : IOperationFilter
    {
        public virtual void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
        {
            if (typeof(SingleResult).IsAssignableFrom(apiDescription.ResponseType()) && apiDescription.HttpMethod != HttpMethod.Get)
            {
                operation.parameters.Add(new Parameter { name = "$expand", @in = "query", description = "Expands related entities inline.", type = "string", required = false });
                operation.parameters.Add(new Parameter { name = "$select", @in = "query", description = "Selects which properties to include in the response.", type = "string", required = false });
            }
        }
    }
}
