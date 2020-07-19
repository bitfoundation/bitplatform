using Bit.Model.Implementations;
using System.Reflection;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetUniversalAutoMapperAssembly(this AssemblyContainer container)
        {
            return typeof(DefaultDtoEntityMapper<,>).GetTypeInfo().Assembly;
        }
    }
}
