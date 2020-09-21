using Bit.Hangfire.Implementations;
using System.Reflection;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetServerHangfireAssembly(this AssemblyContainer container)
        {
            return typeof(HangfireBackgroundJobWorker).GetTypeInfo().Assembly;
        }
    }
}
