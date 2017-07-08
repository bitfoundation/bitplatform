using Bit.Core;
using Bit.OData.Contracts;
using System;
using System.Web.OData.Builder;

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
            _autoEdmBuilder.AutoBuildEdmFromAssembly(AssemblyContainer.Current.GetBitChangeSetManagerODataAssembly(), edmModelBuilder);
        }

        public virtual string GetEdmName()
        {
            return "BitChangeSetManager";
        }
    }
}
