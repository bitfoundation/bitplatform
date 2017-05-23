using Foundation.DataAccess.Implementations.EntityFrameworkCore;
using System.Reflection;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetBitDataEntityFrameworkCoreAssembly(this AssemblyContainer container)
        {
            return typeof(EfRepository<>).GetTypeInfo().Assembly;
        }
    }
}
