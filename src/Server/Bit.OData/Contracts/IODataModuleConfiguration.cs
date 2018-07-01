using Microsoft.AspNet.OData.Builder;
using System.Reflection;

namespace Bit.OData.Contracts
{
    public interface IODataModuleConfiguration
    {
        void ConfigureODataModule(string odataRouteName, Assembly odataAssembly, ODataModelBuilder odataModelBuilder);
    }
}
