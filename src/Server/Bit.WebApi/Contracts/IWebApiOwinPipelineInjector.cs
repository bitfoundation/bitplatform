using System.Web.Http;
using Owin;

namespace Bit.WebApi.Contracts
{
    public interface IWebApiOwinPipelineInjector
    {
        void UseWebApi(IAppBuilder owinApp, HttpServer server, HttpConfiguration webApiConfiguration);
    }
}
