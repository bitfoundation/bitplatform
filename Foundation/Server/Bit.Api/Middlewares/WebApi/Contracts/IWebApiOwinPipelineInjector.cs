using Owin;
using System.Web.Http;

namespace Foundation.Api.Middlewares.WebApi.Contracts
{
    public interface IWebApiOwinPipelineInjector
    {
        void UseWebApiOData(IAppBuilder owinApp, HttpServer server);
    }
}
