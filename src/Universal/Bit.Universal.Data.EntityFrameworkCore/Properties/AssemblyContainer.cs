using Bit.Data.EntityFrameworkCore.Implementations;
using System.Reflection;

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
