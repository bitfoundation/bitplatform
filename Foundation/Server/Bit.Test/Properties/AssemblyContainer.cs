using Foundation.Test;
using System.Reflection;

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
