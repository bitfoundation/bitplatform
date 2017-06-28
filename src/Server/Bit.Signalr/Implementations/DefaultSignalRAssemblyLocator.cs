using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNet.SignalR.Hubs;

namespace Bit.Signalr.Implementations
{
    public class DefaultSignalRAssemblyLocator : IAssemblyLocator
    {
        private readonly Assembly[] _assemblies;

        public DefaultSignalRAssemblyLocator(Assembly[] assemblies)
        {
            if (assemblies == null)
                throw new ArgumentNullException(nameof(assemblies));

            _assemblies = assemblies;
        }

        protected DefaultSignalRAssemblyLocator()
        {

        }

        public virtual IList<Assembly> GetAssemblies()
        {
            return _assemblies;
        }
    }
}