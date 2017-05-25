using System.Web.Http;
using Owin;

namespace Bit.Api.Middlewares.WebApi.Contracts
{
    public interface IWebApiOwinPipelineInjector
    {
        void UseWebApiOData(IAppBuilder owinApp, HttpServer server);
    }
}
