using Bit.Core;
using Bit.OData.Contracts;
using System.Reflection;
using System.Web.OData.Builder;

namespace Bit.OData.Implementations
{
    public abstract class DefaultODataModelBuilder : IODataModelBuilder
    {
#if DEBUG
        protected DefaultODataModelBuilder()
        {

        }
#endif

        private readonly IAutoODataModelBuilder _autoEdmBuilder;

        public DefaultODataModelBuilder(IAutoODataModelBuilder autoEdmBuilder)
        {
            _autoEdmBuilder = autoEdmBuilder;
        }

        public virtual void BuildModel(ODataModelBuilder odataModelBuilder)
        {
            foreach (Assembly appAssembly in AssemblyContainer.Current.AssembliesWithDefaultAssemblies())
            {
                _autoEdmBuilder.AutoBuildODatModelFromAssembly(appAssembly, odataModelBuilder);
            }
        }

        public abstract string GetODataRoute();
    }
}
