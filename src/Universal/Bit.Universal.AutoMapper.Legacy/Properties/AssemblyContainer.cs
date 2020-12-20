using Bit.Model.Implementations;
using System.Reflection;

#if Android || iOS
[assembly: Bit.Client.Xamarin.Preserve]
#endif

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
