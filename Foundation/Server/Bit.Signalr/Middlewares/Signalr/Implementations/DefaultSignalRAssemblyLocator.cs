using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNet.SignalR.Hubs;
using System;

namespace Foundation.Api.Middlewares.SignalR.Implementations
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