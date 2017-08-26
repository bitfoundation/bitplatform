using Bit.Core;
using Bit.OData.Contracts;
using System;
using System.Web.OData.Builder;

namespace BitChangeSetManager.Api.Implementations
{
    public class BitChangeSetManagerODataServiceBuilder : IODataServiceBuilder
    {
        private readonly IAutoODataModelBuilder _autoEdmBuilder;

        public BitChangeSetManagerODataServiceBuilder(IAutoODataModelBuilder autoEdmBuilder)
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
