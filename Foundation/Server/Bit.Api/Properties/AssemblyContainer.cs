using System.Reflection;
using Bit.Api.ApiControllers;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetBitApiAssembly(this AssemblyContainer container)
        {
            return typeof(DtoController<>).GetTypeInfo().Assembly;
        }
    }
}
