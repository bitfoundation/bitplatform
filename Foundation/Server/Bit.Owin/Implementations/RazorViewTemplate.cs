using System;
using System.Collections.Generic;
using Bit.Core.Contracts;
using RazorEngine.Templating;

namespace Bit.Owin.Implementations
{
    public class RazorViewTemplate : TemplateBase<IDependencyResolver>
    {
        protected virtual TContract Resolve<TContract>(string name = null)
        {
            if (name != null)
                throw new NotSupportedException();

            return Model.Resolve<TContract>();
        }

        protected virtual IEnumerable<TContract> ResolveAll<TContract>(string name = null)
        {
            if (name != null)
                throw new NotSupportedException();

            return Model.Resolve<IEnumerable<TContract>>();
        }
    }
}
