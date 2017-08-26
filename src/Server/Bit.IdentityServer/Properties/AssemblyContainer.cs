using System.Reflection;
using Bit.IdentityServer;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetBitIdentityServerAssembly(this AssemblyContainer container)
        {
            return typeof(IdentityServerMiddlewareConfiguration).GetTypeInfo().Assembly;
        }
    }
}
