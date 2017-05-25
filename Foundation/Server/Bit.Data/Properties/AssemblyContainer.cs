using System.Reflection;
using Bit.Data;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetBitDataAssembly(this AssemblyContainer container)
        {
            return typeof(DefaultSqlDbConnectionProvider).GetTypeInfo().Assembly;
        }
    }
}
