using System.Reflection;

namespace Bit.Core
{
    public class AssemblyContainer
    {
        public static AssemblyContainer Current { get; } = new AssemblyContainer();
    }

    public static class AssemblyContainerExtensions
    {
        public static Assembly GetBitCoreAssembly(this AssemblyContainer container)
        {
            return typeof(AssemblyContainer).GetTypeInfo().Assembly;
        }
    }
}
