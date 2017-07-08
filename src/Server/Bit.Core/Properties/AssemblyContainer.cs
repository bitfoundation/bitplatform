using System.Reflection;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetBitCoreAssembly(this AssemblyContainer container)
        {
            return typeof(AssemblyContainer).GetTypeInfo().Assembly;
        }
    }
}
