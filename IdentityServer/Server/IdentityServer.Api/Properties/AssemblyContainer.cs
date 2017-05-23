using IdentityServer.Api.Implementations;
using System.Reflection;

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
