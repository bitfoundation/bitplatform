using Swashbuckle.Swagger;
using System;
using System.Linq;
using System.Web.Http.Description;

namespace Bit.WebApi.Implementations
{
    /// <summary>
    /// Represents the Swagger/Swashbuckle operation filter used to provide default values.
    /// </summary>
    /// <remarks>This <see cref="IOperationFilter"/> is only required due to bugs in the <see cref="SwaggerGenerator"/>.
    /// Once they are fixed and published, this class can be removed.</remarks>
    public class SwaggerDefaultValuesOperationFilter : IOperationFilter
    {
        /// <summary>
        /// Applies the filter to the specified operation using the given context.
        /// </summary>
        /// <param name="operation">The operation to apply the filter to.</param>
        /// <param name="schemaRegistry">The API schema registry.</param>
        /// <param name="apiDescription">The API description being filtered.</param>
        public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            if (schemaRegistry == null)
                throw new ArgumentNullException(nameof(schemaRegistry));

            if (apiDescription == null)
                throw new ArgumentNullException(nameof(apiDescription));

            if (operation.parameters == null)
            {
                return;
            }

            foreach (Parameter parameter in operation.parameters)
            {
                var parameterName = parameter.name;

                if (parameterName.Contains('.', StringComparison.InvariantCultureIgnoreCase))
                    parameterName = parameter.name.Split('.')[0];

                ApiParameterDescription description = apiDescription.ParameterDescriptions.First(p => p.Name == parameterName);

                // REF: https://github.com/domaindrivendev/Swashbuckle/issues/1101
                if (parameter.description == null)
                {
                    parameter.description = description.Documentation;
                }

                // REF: https://github.com/domaindrivendev/Swashbuckle/issues/1089
                // REF: https://github.com/domaindrivendev/Swashbuckle/pull/1090
                if (parameter.@default == null)
                {
                    parameter.@default = description.ParameterDescriptor?.DefaultValue;
                }
            }
        }
    }
}
