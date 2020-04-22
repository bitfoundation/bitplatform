using System.Reflection;
using Bit.OData;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetServerODataAssembly(this AssemblyContainer container)
        {
            return typeof(WebApiODataMiddlewareConfiguration).GetTypeInfo().Assembly;
        }
    }
}
