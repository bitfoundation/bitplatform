using Foundation.AspNetCore.Test;
using System.Reflection;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetBitTestCoreAssembly(this AssemblyContainer container)
        {
            return typeof(AspNetCoreTestEnvironment).GetTypeInfo().Assembly;
        }
    }
}
