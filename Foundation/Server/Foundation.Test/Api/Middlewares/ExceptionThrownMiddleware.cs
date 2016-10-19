using System;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace Foundation.Test.Api.Middlewares
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
