using System;
using System.Reflection;
using System.Web.OData.Builder;
using Bit.OData.Contracts;
using Bit.OData.ODataControllers;

namespace Bit.OData.Implementations
{
    public class BitODataServiceBuilder : IODataServiceBuilder
    {
        private readonly IAutoODataModelBuilder _autoODataModelBuilder;

        public BitODataServiceBuilder(IAutoODataModelBuilder autoODataModelBuilder)
        {
            if (autoODataModelBuilder == null)
                throw new ArgumentNullException(nameof(autoODataModelBuilder));

            _autoODataModelBuilder = autoODataModelBuilder;
        }

#if DEBUG
        protected BitODataServiceBuilder()
        {
        }
#endif

        public virtual void BuildModel(ODataModelBuilder oDataModelBuilder)
        {
            _autoODataModelBuilder.AutoBuildODataModelFromTypes(new[] { typeof(ClientsLogsController).GetTypeInfo(), typeof(JobsInfoController).GetTypeInfo(), typeof(UsersSettingsController).GetTypeInfo() }, oDataModelBuilder);
        }

        public virtual string GetODataRoute()
        {
            return "Bit";
        }
    }
}