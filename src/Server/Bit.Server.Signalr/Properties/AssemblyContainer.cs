using Bit.Signalr;
using System.Reflection;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetServerSignalRAssembly(this AssemblyContainer container)
        {
            return typeof(MessagesHub).GetTypeInfo().Assembly;
        }
    }
}
