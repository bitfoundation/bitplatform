using Bit.Data.Contracts;
using System.Reflection;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetServerDataAssembly(this AssemblyContainer container)
        {
            return typeof(IUnitOfWork).GetTypeInfo().Assembly;
        }
    }
}
