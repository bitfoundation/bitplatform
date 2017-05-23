using Foundation.Model.Contracts;
using System.Reflection;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetBitModelAssembly(this AssemblyContainer container)
        {
            return typeof(IEntity).GetTypeInfo().Assembly;
        }
    }
}
