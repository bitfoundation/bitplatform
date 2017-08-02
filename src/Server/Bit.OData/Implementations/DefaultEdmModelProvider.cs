using Bit.Core;
using Bit.OData.Contracts;
using System.Reflection;
using System.Web.OData.Builder;

namespace Bit.OData.Implementations
{
    public abstract class DefaultEdmModelProvider : IEdmModelProvider
    {
#if DEBUG
        protected DefaultEdmModelProvider()
        {

        }
#endif

        private readonly IAutoEdmBuilder _autoEdmBuilder;

        public DefaultEdmModelProvider(IAutoEdmBuilder autoEdmBuilder)
        {
            _autoEdmBuilder = autoEdmBuilder;
        }

        public virtual void BuildEdmModel(ODataModelBuilder edmModelBuilder)
        {
            foreach (Assembly appAssembly in AssemblyContainer.Current.AssembliesWithDefaultAssemblies())
            {
                _autoEdmBuilder.AutoBuildEdmFromAssembly(appAssembly, edmModelBuilder);
            }
        }

        public abstract string GetEdmName();
    }
}
