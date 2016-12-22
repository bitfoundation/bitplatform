using System;

namespace Foundation.Test.Server
{
    public class TestServerFactory
    {
        static TestServerFactory()
        {
            GetEmbeddedTestServer = () => new OwinEmbeddedTestServer();

            GetSelfHostTestServer = () => new OwinSelfHostTestServer();
        }

        public static Func<ITestServer> GetEmbeddedTestServer { get; set; }

        public static Func<ITestServer> GetSelfHostTestServer { get; set; }
    }
}
