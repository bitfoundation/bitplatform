using Bit.Http.Contracts;
using System.Reflection;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetUniversalHttpAssembly(this AssemblyContainer container)
        {
            return typeof(Token).GetTypeInfo().Assembly;
        }
    }
}
