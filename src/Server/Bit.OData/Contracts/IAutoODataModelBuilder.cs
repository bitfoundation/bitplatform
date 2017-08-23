using System.Collections.Generic;
using System.Reflection;
using System.Web.OData.Builder;

namespace Bit.OData.Contracts
{
    public interface IAutoODataModelBuilder
    {
        void AutoBuildODataModelFromTypes(IEnumerable<TypeInfo> controllers, ODataModelBuilder modelBuilder);

        void AutoBuildODatModelFromAssembly(Assembly assembly, ODataModelBuilder modelBuilder);
    }
}
