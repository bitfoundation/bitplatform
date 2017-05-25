using System;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace Bit.Tests.Api.Middlewares
{
    public class ExceptionThrownMiddleware : OwinMiddleware
    {
        public ExceptionThrownMiddleware(OwinMiddleware next) :
            base(next)
        {

        }

        public override Task Invoke(IOwinContext context)
        {
            throw new InvalidOperationException();
        }
    }
}
