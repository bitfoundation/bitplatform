using System.Reflection;
using Bit.WebApi;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetBitWebApiAssembly(this AssemblyContainer container)
        {
            return typeof(WebApiMiddlewareConfiguration).GetTypeInfo().Assembly;
        }
    }
}
