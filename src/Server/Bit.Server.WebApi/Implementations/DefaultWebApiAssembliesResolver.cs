using Bit.WebApi.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Http.Dispatcher;

namespace Bit.WebApi.Implementations
{
    public class DefaultWebApiAssembliesResolver : IAssembliesResolver
    {
        public virtual IEnumerable<IApiAssembliesProvider> ApiAssembliesProviders { get; set; } = default!;

        private ICollection<Assembly>? _result;

        public virtual ICollection<Assembly> GetAssemblies()
        {
            if (_result == null)
            {
                _result = ApiAssembliesProviders
                    .SelectMany(asmProvider => asmProvider.GetApiAssemblies())
                    .ToList();
            }

            return _result;
        }
    }
}