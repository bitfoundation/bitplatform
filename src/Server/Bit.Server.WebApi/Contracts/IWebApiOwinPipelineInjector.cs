using Owin;
using System.Web.Http;

namespace Bit.WebApi.Contracts
{
    public interface IWebApiOwinPipelineInjector
    {
        void UseWebApi(IAppBuilder owinApp, HttpServer server, HttpConfiguration webApiConfiguration);
    }
}
