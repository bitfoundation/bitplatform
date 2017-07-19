using System.Reflection;
using Bit.Data.EntityFrameworkCore.Implementations;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetBitDataEntityFrameworkCoreAssembly(this AssemblyContainer container)
        {
            return typeof(EfCoreRepository<>).GetTypeInfo().Assembly;
        }
    }
}
