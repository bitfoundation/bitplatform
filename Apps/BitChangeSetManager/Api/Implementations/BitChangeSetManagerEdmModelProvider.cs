using Bit.Api.Contracts.Project;
using Bit.Api.Middlewares.WebApi.OData.Contracts;
using System;
using System.Reflection;
using System.Web.OData.Builder;
using Bit.Core;

namespace BitChangeSetManager.Api.Implementations
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
            _autoEdmBuilder.AutoBuildEdmFromAssembly(AssemblyContainer.Current.GetBitChangeSetManagerApiAssembly(), edmModelBuilder);
        }

        public virtual string GetEdmName()
        {
            return "BitChangeSetManager";
        }
    }
}
