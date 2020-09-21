using Bit.Test;
using System.Reflection;

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
