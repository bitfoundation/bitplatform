using System.Reflection;
using Bit.Api.ApiControllers;
using BitChangeSetManager;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetBitChangeSetManagerApiAssembly(this AssemblyContainer container)
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
