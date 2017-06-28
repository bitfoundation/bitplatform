using System.Collections.Generic;
using System.Reflection;
using System.Web.OData.Builder;

namespace Bit.OData.Contracts
{
    public interface IAutoEdmBuilder
    {
        void AutoBuildEdmFromTypes(IEnumerable<TypeInfo> controllers, ODataModelBuilder modelBuilder);

        void AutoBuildEdmFromAssembly(Assembly assembly, ODataModelBuilder modelBuilder);
    }
}
