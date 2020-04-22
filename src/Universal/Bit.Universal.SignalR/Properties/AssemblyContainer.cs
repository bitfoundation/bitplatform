using Bit.Signalr.Implementations;
using System.Reflection;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetUniversalODataAssembly(this AssemblyContainer container)
        {
            return typeof(DefaultServerSentEventsTransport).GetTypeInfo().Assembly;
        }
    }
}
