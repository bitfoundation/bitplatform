using Bit.WebApi.Contracts;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Bit.WebApi.Implementations
{
    public class DefaultWebApiAssembliesProvider : IApiAssembliesProvider
    {
        protected DefaultWebApiAssembliesProvider()
        {

        }

        private readonly IEnumerable<Assembly> _assemblies;

        public DefaultWebApiAssembliesProvider(IEnumerable<Assembly> assemblies)
        {
            _assemblies = assemblies ?? throw new ArgumentNullException(nameof(assemblies));
        }

        public virtual IEnumerable<Assembly> GetApiAssemblies()
        {
            return _assemblies;
        }
    }
}
