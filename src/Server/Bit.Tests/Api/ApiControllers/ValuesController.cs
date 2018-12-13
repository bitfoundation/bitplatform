using System.Collections.Generic;
using System.Web.Http;

namespace Bit.Tests.Api.ApiControllers
{
    public class ValuesController : ApiController
    {
        public virtual IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }
    }
}
