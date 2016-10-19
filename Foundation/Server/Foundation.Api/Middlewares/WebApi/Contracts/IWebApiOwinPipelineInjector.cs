using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Foundation.Api.Middlewares.WebApi.Contracts
{
    public interface IWebApiOwinPipelineInjector
    {
        void UseWebApiOData(IAppBuilder owinApp, HttpServer server);
    }
}
