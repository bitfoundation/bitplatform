using System.Reflection;
using Bit.Owin.Contracts;
using Bit.OwinCore;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetServerOwinAssembly(this AssemblyContainer container)
        {
            return typeof(IOwinMiddlewareConfiguration).GetTypeInfo().Assembly;
        }

        public static Assembly GetServerMetadataAssembly(this AssemblyContainer container)
        {
            return typeof(IOwinMiddlewareConfiguration).GetTypeInfo().Assembly;
        }
    }
}
