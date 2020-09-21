using Bit.WebApi;
using System.Reflection;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetServerWebApiAssembly(this AssemblyContainer container)
        {
            return typeof(WebApiMiddlewareConfiguration).GetTypeInfo().Assembly;
        }
    }
}
