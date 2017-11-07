using System.Reflection;
using System.Web.OData.Builder;
using Bit.OData.Contracts;
using Bit.OData.ODataControllers;

namespace Bit.OData.Implementations
{
    public class BitODataServiceBuilder : IODataServiceBuilder
    {
        public virtual IAutoODataModelBuilder AutoODataModelBuilder { get; set; }

        public virtual void BuildModel(ODataModelBuilder oDataModelBuilder)
        {
            AutoODataModelBuilder.AutoBuildODataModelFromTypes(new[] { typeof(ClientsLogsController).GetTypeInfo(), typeof(JobsInfoController).GetTypeInfo(), typeof(UsersSettingsController).GetTypeInfo() }, oDataModelBuilder);
        }

        public virtual string GetODataRoute()
        {
            return "Bit";
        }
    }
}