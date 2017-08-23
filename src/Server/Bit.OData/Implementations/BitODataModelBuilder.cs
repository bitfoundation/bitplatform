using System;
using System.Reflection;
using System.Web.OData.Builder;
using Bit.OData.Contracts;
using Bit.OData.ODataControllers;

namespace Bit.OData.Implementations
{
    public class BitODataModelBuilder : IODataModelBuilder
    {
        private readonly IAutoODataModelBuilder _autoODataModelBuilder;

        public BitODataModelBuilder(IAutoODataModelBuilder autoODataModelBuilder)
        {
            if (autoODataModelBuilder == null)
                throw new ArgumentNullException(nameof(autoODataModelBuilder));

            _autoODataModelBuilder = autoODataModelBuilder;
        }

#if DEBUG
        protected BitODataModelBuilder()
        {
        }
#endif

        public virtual void BuildModel(ODataModelBuilder odataModelBuilder)
        {
            _autoODataModelBuilder.AutoBuildODataModelFromTypes(new[] { typeof(ClientsLogsController).GetTypeInfo(), typeof(JobsInfoController).GetTypeInfo(), typeof(UsersSettingsController).GetTypeInfo() }, odataModelBuilder);
        }

        public virtual string GetODataRoute()
        {
            return "Bit";
        }
    }
}