using Bit.Model.Implementations;
using System.Reflection;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetUniversalAutoMapperLegacyAssembly(this AssemblyContainer container)
        {
            return typeof(DefaultDtoEntityMapper<,>).GetTypeInfo().Assembly;
        }
    }
}
