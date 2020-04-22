using System.Reflection;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetUniversalCoreAssembly(this AssemblyContainer container)
        {
            return typeof(AssemblyContainer).GetTypeInfo().Assembly;
        }
    }
}
