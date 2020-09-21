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

        private readonly Assembly[] _assemblies = default!;

        public DefaultSignalRAssemblyLocator(Assembly[] assemblies)
        {
            if (assemblies == null)
                throw new ArgumentNullException(nameof(assemblies));

            _assemblies = assemblies;
        }

        public virtual IList<Assembly> GetAssemblies()
        {
            return _assemblies;
        }
    }
}