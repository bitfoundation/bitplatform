using Bit.Model.Contracts;
using System.Reflection;

[assembly: Bit.Client.Xamarin.Preserve]

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetUniversalModelAssembly(this AssemblyContainer container)
        {
            return typeof(IEntity).GetTypeInfo().Assembly;
        }
    }
}
