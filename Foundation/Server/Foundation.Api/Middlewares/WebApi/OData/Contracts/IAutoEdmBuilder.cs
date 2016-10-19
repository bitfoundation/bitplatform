using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.OData.Builder;

namespace Foundation.Api.Middlewares.WebApi.OData.Contracts
{
    public interface IAutoEdmBuilder
    {
        void AutoBuildEdmFromTypes(IEnumerable<TypeInfo> controllers, ODataModelBuilder modelBuilder);

        void AutoBuildEdmFromAssembly(Assembly assembly, ODataModelBuilder modelBuilder);
    }
}
