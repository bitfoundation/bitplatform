using Bit.Data.EntityFrameworkCore.Implementations;
using System.Reflection;

#if Android || iOS
[assembly: Bit.Client.Xamarin.Preserve]
#endif

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly UniversalDataEntityFrameworkCoreAssembly(this AssemblyContainer container)
        {
            return typeof(EfCoreRepositoryBase<>).GetTypeInfo().Assembly;
        }
    }
}
