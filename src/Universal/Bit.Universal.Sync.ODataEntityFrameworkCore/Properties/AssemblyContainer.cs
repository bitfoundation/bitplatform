using Bit.Sync.ODataEntityFrameworkCore.Implementations;
using System.Reflection;

[assembly: Bit.Client.Xamarin.Preserve]

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
