using System.Reflection;
using Bit.Test;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetBitTestAssembly(this AssemblyContainer container)
        {
            return typeof(TestEnvironmentBase).GetTypeInfo().Assembly;
        }
    }
}
