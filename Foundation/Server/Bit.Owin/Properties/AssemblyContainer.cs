using Foundation.Api.Contracts;
using System.Reflection;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetBitOwinAssembly(this AssemblyContainer container)
        {
            return typeof(IOwinMiddlewareConfiguration).GetTypeInfo().Assembly;
        }
    }
}
