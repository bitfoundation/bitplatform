using Bit.Sync.ODataEntityFrameworkCore.Implementations;
using System.Reflection;

#if Android || iOS
[assembly: Bit.Client.Xamarin.Preserve]
#endif

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetUniversalSyncODataEntityFrameworkCoreAssembly(this AssemblyContainer container)
        {
            return typeof(DefaultSyncService).GetTypeInfo().Assembly;
        }
    }
}
