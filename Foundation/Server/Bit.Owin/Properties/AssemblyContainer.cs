using System.Reflection;
using Bit.Owin.Contracts;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetBitOwinAssembly(this AssemblyContainer container)
        {
            return typeof(IOwinMiddlewareConfiguration).GetTypeInfo().Assembly;
        }

        public static Assembly GetBitMetadataAssembly(this AssemblyContainer container)
        {
            return typeof(IOwinMiddlewareConfiguration).GetTypeInfo().Assembly;
        }
    }
}
