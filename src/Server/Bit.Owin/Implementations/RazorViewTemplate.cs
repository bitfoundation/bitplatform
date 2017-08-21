using System;
using System.Collections.Generic;
using Bit.Core.Contracts;
using RazorEngine.Templating;

namespace Bit.Owin.Implementations
{
    public class RazorViewTemplate : TemplateBase<IDependencyResolver>
    {
        protected virtual TService Resolve<TService>(string name = null)
        {
            if (name != null)
                throw new NotSupportedException();

            return Model.Resolve<TService>();
        }

        protected virtual IEnumerable<TService> ResolveAll<TService>(string name = null)
        {
            if (name != null)
                throw new NotSupportedException();

            return Model.Resolve<IEnumerable<TService>>();
        }
    }
}
