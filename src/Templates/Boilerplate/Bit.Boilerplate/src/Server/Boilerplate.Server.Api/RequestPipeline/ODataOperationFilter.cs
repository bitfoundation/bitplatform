using Microsoft.OpenApi;
using Microsoft.AspNetCore.Mvc.Controllers;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Boilerplate.Server.Api.RequestPipeline;

/// <summary>
/// https://docs.microsoft.com/en-us/odata/concepts/queryoptions-overview
/// </summary>
public partial class ODataOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= [];

        if (context.ApiDescription.ActionDescriptor is not ControllerActionDescriptor descriptor)
            return;

        var odataQueryOptionsParameter = descriptor!.Parameters.SingleOrDefault(p => typeof(ODataQueryOptions).IsAssignableFrom(p.ParameterType));

        if (descriptor != null && descriptor.FilterDescriptors.Any(filter => filter.Filter is EnableQueryAttribute) || odataQueryOptionsParameter is not null)
        {
            operation.Parameters.Add(new OpenApiParameter()
            {
                Name = "$select",
                In = ParameterLocation.Query,
                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.String
                },
                Description = "Returns only the selected properties. (ex. FirstName, LastName)",
                Required = false
            });

            operation.Parameters.Add(new OpenApiParameter()
            {
                Name = "$expand",
                In = ParameterLocation.Query,
                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.String
                },
                Description = "Include only the selected objects. (ex. Orders, Locations)",
                Required = false
            });

            operation.Parameters.Add(new OpenApiParameter()
            {
                Name = "$filter",
                In = ParameterLocation.Query,
                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.String
                },
                Description = "Filter the response with OData filter queries.",
                Required = false
            });

            operation.Parameters.Add(new OpenApiParameter()
            {
                Name = "$search",
                In = ParameterLocation.Query,
                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.String
                },
                Description = "Filter the response with OData search queries.",
                Required = false
            });

            operation.Parameters.Add(new OpenApiParameter()
            {
                Name = "$top",
                In = ParameterLocation.Query,
                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.Integer
                },
                Description = "Number of objects to return. (ex. 25)",
                Required = false
            });

            operation.Parameters.Add(new OpenApiParameter()
            {
                Name = "$skip",
                In = ParameterLocation.Query,
                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.Integer
                },
                Description = "Number of objects to skip in the current order (ex. 50)",
                Required = false
            });

            operation.Parameters.Add(new OpenApiParameter()
            {
                Name = "$orderby",
                In = ParameterLocation.Query,
                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.String
                },
                Description = "Define the order by one or more fields (ex. LastModified)",
                Required = false
            });
        }

        if (odataQueryOptionsParameter is not null)
        {
            operation.Parameters.Remove(operation.Parameters.Single(p => p.Name == odataQueryOptionsParameter.Name));
        }
    }
}
