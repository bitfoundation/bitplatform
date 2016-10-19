using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Foundation.Test.Server
{
    public class TestServerFactory
    {
        static TestServerFactory()
        {
            GetEmbeddedTestServer = () =>
            {
                return new OwinEmbeddedTestServer();
            };

            GetSelfHostTestServer = () =>
            {
                return new OwinSelfHostTestServer();
            };
        }

        public static Func<ITestServer> GetEmbeddedTestServer { get; set; }

        public static Func<ITestServer> GetSelfHostTestServer { get; set; }
    }
}
