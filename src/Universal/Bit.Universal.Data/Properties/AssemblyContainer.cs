using Bit.Data.Contracts;
using System.Reflection;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetUniversalDataAssembly(this AssemblyContainer container)
        {
            return typeof(IRepository<>).GetTypeInfo().Assembly;
        }
    }
}
