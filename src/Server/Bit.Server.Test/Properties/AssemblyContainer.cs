using System.Reflection;
using Bit.Test;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetServerTestAssembly(this AssemblyContainer container)
        {
            return typeof(TestEnvironmentBase).GetTypeInfo().Assembly;
        }
    }
}
