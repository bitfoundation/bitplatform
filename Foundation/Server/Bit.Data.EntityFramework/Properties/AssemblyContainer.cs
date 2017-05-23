using Bit.Data.EntityFramework.Implementations;
using System.Reflection;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetBitDataEntityFrameworkAssembly(this AssemblyContainer container)
        {
            return typeof(EfRepository<>).GetTypeInfo().Assembly;
        }
    }
}
