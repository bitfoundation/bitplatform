using Bit.Http.Contracts;
using System.Reflection;

#if Android || iOS
[assembly: Bit.Client.Xamarin.Preserve]
#endif

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetUniversalHttpAssembly(this AssemblyContainer container)
        {
            return typeof(Token).GetTypeInfo().Assembly;
        }
    }
}
