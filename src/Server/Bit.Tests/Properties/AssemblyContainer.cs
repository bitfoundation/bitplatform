using Bit.Tests;
using System.Reflection;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetBitTestsAssembly(this AssemblyContainer container)
        {
            return typeof(BitOwinTestEnvironment).GetTypeInfo().Assembly;
        }
    }
}
