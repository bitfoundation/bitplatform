using Foundation.Api.Implementations;
using System.Reflection;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetBitHangfireAssembly(this AssemblyContainer container)
        {
            return typeof(DefaultBackgroundJobWorker).GetTypeInfo().Assembly;
        }
    }
}
