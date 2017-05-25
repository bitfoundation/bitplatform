using System.Reflection;
using Bit.IdentityServer.Implementations;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetBitIdentityServerAssembly(this AssemblyContainer container)
        {
            return typeof(ActiveDirectoryUserServiceProvider).GetTypeInfo().Assembly;
        }
    }
}
