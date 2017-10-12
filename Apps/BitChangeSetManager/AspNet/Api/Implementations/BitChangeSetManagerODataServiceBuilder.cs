using Bit.Core;
using Bit.OData.Contracts;
using System;
using System.Web.OData.Builder;

namespace BitChangeSetManager.Api.Implementations
{
    public class BitChangeSetManagerODataServiceBuilder : IODataServiceBuilder
    {
        public virtual IAutoODataModelBuilder AutoEdmBuilder { get; set; }

        public virtual void BuildModel(ODataModelBuilder edmModelBuilder)
        {
            AutoEdmBuilder.AutoBuildODatModelFromAssembly(AssemblyContainer.Current.GetBitChangeSetManagerODataAssembly(), edmModelBuilder);
        }

        public virtual string GetODataRoute()
        {
            return "BitChangeSetManager";
        }
    }
}
