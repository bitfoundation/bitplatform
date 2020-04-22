using Bit.Core.Contracts;
using System.Reflection;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetServerCoreAssembly(this AssemblyContainer container)
        {
            return typeof(ILogger).GetTypeInfo().Assembly;
        }
    }
}
