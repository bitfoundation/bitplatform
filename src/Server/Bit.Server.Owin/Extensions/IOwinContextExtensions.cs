using Bit.Core.Contracts;
using Bit.Owin.Middlewares;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Owin
{
    public static class IOwinContextExtensions
    {
        public static IDependencyResolver GetDependencyResolver(this IOwinContext context)
        {
            if (context.TryGetDependencyResolver(out IDependencyResolver? dependencyResolver))
                return dependencyResolver;
            throw new InvalidOperationException($"DependencyResolver not found in owin context, See {nameof(AutofacScopeBasedDependencyResolverMiddleware)}");
        }

        public static bool TryGetDependencyResolver(this IOwinContext context, [MaybeNullWhen(returnValue: false)] out IDependencyResolver dependencyResolver)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.Request.CallCancelled.ThrowIfCancellationRequested();

            dependencyResolver = context.Get<IDependencyResolver>("DependencyResolver");

            if (dependencyResolver == default(IDependencyResolver))
                return false;

            return true;
        }
    }
}
