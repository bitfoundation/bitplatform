using Foundation.Api.Contracts.Project;
using Foundation.Api.Middlewares.WebApi.OData.Contracts;
using System;
using System.Reflection;
using System.Web.OData.Builder;

namespace BitChangeSetManager.Api
{
    public class BitChangeSetManagerEdmModelProvider : IEdmModelProvider
    {
        private readonly IAutoEdmBuilder _autoEdmBuilder;

        public BitChangeSetManagerEdmModelProvider(IAutoEdmBuilder autoEdmBuilder)
        {
            if (autoEdmBuilder == null)
                throw new ArgumentNullException(nameof(autoEdmBuilder));

            _autoEdmBuilder = autoEdmBuilder;
        }

        public virtual void BuildEdmModel(ODataModelBuilder edmModelBuilder)
        {
            _autoEdmBuilder.AutoBuildEdmFromAssembly(typeof(BitChangeSetManagerEdmModelProvider).GetTypeInfo().Assembly, edmModelBuilder);
        }

        public virtual string GetEdmName()
        {
            return "BitChangeSetManager";
        }
    }
}
