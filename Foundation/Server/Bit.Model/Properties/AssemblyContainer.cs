using System.Reflection;
using Bit.Model.Contracts;

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
