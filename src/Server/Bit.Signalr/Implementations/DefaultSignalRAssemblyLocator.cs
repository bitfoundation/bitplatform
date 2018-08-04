using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Bit.Signalr.Implementations
{
    public class DefaultSignalRAssemblyLocator : IAssemblyLocator
    {
        protected DefaultSignalRAssemblyLocator()
        {

        }

        private readonly Assembly[] _assemblies;

        public DefaultSignalRAssemblyLocator(Assembly[] assemblies)
        {
            _assemblies = assemblies ?? throw new ArgumentNullException(nameof(assemblies));
        }

        public virtual IList<Assembly> GetAssemblies()
        {
            return _assemblies;
        }
    }
}
