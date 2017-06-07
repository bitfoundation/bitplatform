using System.Reflection;
using Bit.OwinCore;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetBitOwinCoreAssembly(this AssemblyContainer container)
        {
            return typeof(AspNetCoreAppStartup).GetTypeInfo().Assembly;
        }
    }
}
