using System.Reflection;
using Bit.Tests;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetBitTestsAssembly(this AssemblyContainer container)
        {
            return typeof(TestEnvironment).GetTypeInfo().Assembly;
        }
    }
}
