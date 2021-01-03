using Bit.Model.Contracts;
using System.Reflection;

#if Android || iOS
[assembly: Bit.Client.Xamarin.Preserve]
#endif

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
