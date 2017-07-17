using System;
using System.Reflection;
using System.Web.OData.Builder;
using Bit.OData.Contracts;
using Bit.OData.ODataControllers;

namespace Bit.OData.Implementations
{
    public class BitEdmModelProvider : IEdmModelProvider
    {
        private readonly IAutoEdmBuilder _autoEdmBuilder;

        public BitEdmModelProvider(IAutoEdmBuilder autoEdmBuilder)
        {
            if (autoEdmBuilder == null)
                throw new ArgumentNullException(nameof(autoEdmBuilder));

            _autoEdmBuilder = autoEdmBuilder;
        }

#if DEBUG
        protected BitEdmModelProvider()
        {
        }
#endif

        public virtual void BuildEdmModel(ODataModelBuilder edmModelBuilder)
        {
            _autoEdmBuilder.AutoBuildEdmFromTypes(new[] { typeof(ClientsLogsController).GetTypeInfo(), typeof(JobsInfoController).GetTypeInfo(), typeof(UsersSettingsController).GetTypeInfo() }, edmModelBuilder);
        }

        public virtual string GetEdmName()
        {
            return "Bit";
        }
    }
}