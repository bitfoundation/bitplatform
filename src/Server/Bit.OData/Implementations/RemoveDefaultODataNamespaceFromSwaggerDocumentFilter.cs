using Swashbuckle.Swagger;
using System.Linq;
using System.Web.Http.Description;

namespace Bit.OData.Implementations
{
    public class RemoveDefaultODataNamespaceFromSwaggerDocumentFilter : IDocumentFilter
    {
        public void Apply(SwaggerDocument swaggerDoc, SchemaRegistry schemaRegistry, IApiExplorer apiExplorer)
        {
            swaggerDoc.paths = swaggerDoc.paths
                .ToDictionary(p => p.Key.Replace("Default.", ""), p => p.Value);
        }
    }
}
