using Bit.IdentityServer;
using System.Reflection;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetServerIdentityServerAssembly(this AssemblyContainer container)
        {
            return typeof(IdentityServerMiddlewareConfiguration).GetTypeInfo().Assembly;
        }
    }
}
