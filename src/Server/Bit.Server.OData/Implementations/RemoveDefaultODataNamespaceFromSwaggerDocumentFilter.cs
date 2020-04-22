using Swashbuckle.Swagger;
using System;
using System.Linq;
using System.Web.Http.Description;

namespace Bit.OData.Implementations
{
    public class RemoveDefaultODataNamespaceFromSwaggerDocumentFilter : IDocumentFilter
    {
        public void Apply(SwaggerDocument swaggerDoc, SchemaRegistry schemaRegistry, IApiExplorer apiExplorer)
        {
            if (swaggerDoc == null)
                throw new ArgumentNullException(nameof(swaggerDoc));

            if (schemaRegistry == null)
                throw new ArgumentNullException(nameof(schemaRegistry));

            if (apiExplorer == null)
                throw new ArgumentNullException(nameof(apiExplorer));

            swaggerDoc.paths = swaggerDoc.paths
                .ToDictionary(p => p.Key.Replace("Default.", "", StringComparison.InvariantCultureIgnoreCase), p => p.Value);
        }
    }
}
