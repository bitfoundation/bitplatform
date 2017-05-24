using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Foundation.Test.Api.Middlewares.WebApi.Tests
{
    [TestClass]
    public class WebApiODataCachingTests
    {
        [TestMethod]
        [TestCategory("WebApi"), TestCategory("Caching")]
        public async Task WebApiODataResponsesMustNotBeCacheable()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment())
            {
                HttpResponseMessage getTestModels = await testEnvironment.Server.GetHttpClient()
                        .GetAsync("/odata/Test/TestModels");

                Assert.AreEqual(HttpStatusCode.OK, getTestModels.StatusCode);

                Assert.AreEqual(false, getTestModels.Headers.CacheControl.Public);

                Assert.AreEqual(null, getTestModels.Headers.CacheControl.MaxAge);

                Assert.AreEqual(true, getTestModels.Headers.CacheControl.NoCache);

                Assert.AreEqual(true, getTestModels.Headers.CacheControl.NoStore);

                Assert.AreEqual(true, getTestModels.Headers.CacheControl.MustRevalidate);
            }
        }
    }
}
