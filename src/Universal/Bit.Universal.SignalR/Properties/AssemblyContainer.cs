using Bit.Signalr.Implementations;
using System.Reflection;

#if Android || iOS
[assembly: Bit.Client.Xamarin.Preserve]
#endif

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
