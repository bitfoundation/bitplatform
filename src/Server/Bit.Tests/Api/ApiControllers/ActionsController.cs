using Bit.Core.Contracts;
using System.Web.Http;

namespace Bit.Tests.Api.ApiControllers
{
    [RoutePrefix("actions")]
    public class ActionsController : ApiController
    {
        public virtual ILogger Logger { get; set; }

        [HttpGet, Route("some-action/log={log}")]
        public virtual void SomeAction(bool log)
        {
            Logger.AddLogData("Test", "Test");

            if (log == true)
                Logger.Policy = LogPolicy.Always;
        }
    }
}
