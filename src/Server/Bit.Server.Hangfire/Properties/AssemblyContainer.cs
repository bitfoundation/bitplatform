using System.Reflection;
using Bit.Hangfire.Implementations;

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
