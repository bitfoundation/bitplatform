using Bit.Core;
using Bit.OData.Contracts;
using System;
using System.Web.OData.Builder;

namespace BitChangeSetManager.Api.Implementations
{
    public class BitChangeSetManagerODataModelBuilder : IODataModelBuilder
    {
        private readonly IAutoODataModelBuilder _autoEdmBuilder;

        public BitChangeSetManagerODataModelBuilder(IAutoODataModelBuilder autoEdmBuilder)
        {
            if (autoEdmBuilder == null)
                throw new ArgumentNullException(nameof(autoEdmBuilder));

            _autoEdmBuilder = autoEdmBuilder;
        }

        public virtual void BuildModel(ODataModelBuilder edmModelBuilder)
        {
            _autoEdmBuilder.AutoBuildODatModelFromAssembly(AssemblyContainer.Current.GetBitChangeSetManagerODataAssembly(), edmModelBuilder);
        }

        public virtual string GetODataRoute()
        {
            return "BitChangeSetManager";
        }
    }
}
