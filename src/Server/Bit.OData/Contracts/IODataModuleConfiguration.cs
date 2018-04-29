using System.Reflection;
using System.Web.OData.Builder;

namespace Bit.OData.Contracts
{
    public interface IODataModuleConfiguration
    {
        void ConfigureODataModule(string odataRouteName, Assembly odataAssembly, ODataModelBuilder odataModelBuilder);
    }
}
