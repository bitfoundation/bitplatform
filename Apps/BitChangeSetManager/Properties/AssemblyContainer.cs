using System.Reflection;
using BitChangeSetManager;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetBitChangeSetManagerWebApiAssembly(this AssemblyContainer container)
        {
            return typeof(AppStartup).GetTypeInfo().Assembly;
        }

        public static Assembly GetBitChangeSetManagerODataAssembly(this AssemblyContainer container)
        {
            return typeof(AppStartup).GetTypeInfo().Assembly;
        }

        public static Assembly GetBitChangeSetManagerMetadataAssembly(this AssemblyContainer container)
        {
            return typeof(AppStartup).GetTypeInfo().Assembly;
        }

        public static Assembly GetBitChangeSetManagerSignalrAssembly(this AssemblyContainer container)
        {
            return typeof(AppStartup).GetTypeInfo().Assembly;
        }
    }
}
