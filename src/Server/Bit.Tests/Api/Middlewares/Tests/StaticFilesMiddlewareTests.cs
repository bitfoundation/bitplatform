using Bit.Core.Contracts;
using Bit.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Bit.Tests.Api.Middlewares.Tests
{
    [TestClass]
    public class StaticFilesMiddlewareTests
    {
        [TestMethod]
        [TestCategory("Static Files"), TestCategory("Caching")]
        public async Task StaticFilesResponsesMustBeCacheable()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs
            {
                ActiveAppEnvironmentCustomizer = activeAppEnv => activeAppEnv.DebugMode = false
            }))
            {
                HttpResponseMessage getVirtualPathUrl = await testEnvironment.Server.BuildHttpClient()
                    .GetAsync("/Files/V1");

                Assert.AreEqual(true, getVirtualPathUrl.Headers.CacheControl.Public);

                Assert.AreEqual(TimeSpan.FromDays(365), getVirtualPathUrl.Headers.CacheControl.MaxAge);

                Assert.AreEqual(false, getVirtualPathUrl.Headers.CacheControl.NoCache);

                Assert.AreEqual(false, getVirtualPathUrl.Headers.CacheControl.NoStore);

                Assert.AreEqual(false, getVirtualPathUrl.Headers.CacheControl.MustRevalidate);
            }
        }

        [TestMethod]
        [TestCategory("Static Files"), TestCategory("Security")]
        public async Task StaticFilesResponsesMustHaveSecurityHeaders()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                HttpResponseMessage getVirtualPathUrl = await testEnvironment.Server.BuildHttpClient()
                    .GetAsync("/Files/V1");

                Assert.AreEqual(true, getVirtualPathUrl.Headers.Contains("X-Content-Type-Options"));
                Assert.AreEqual(true, getVirtualPathUrl.Headers.Contains("X-Download-Options"));
            }
        }

        [TestMethod]
        [TestCategory("Static Files"), TestCategory("Security")]
        public async Task StaticFilesShouldNotReturnDirectoryContentsInReleaseMode()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs
            {
                ActiveAppEnvironmentCustomizer =
                environment =>
                {
                    environment.DebugMode = false;
                }
            }))
            {
                HttpResponseMessage getVirtualPathUrl = await testEnvironment.Server.BuildHttpClient()
                    .GetAsync("/Files/V1");

                Assert.AreEqual(HttpStatusCode.NotFound, getVirtualPathUrl.StatusCode);
            }
        }

        [TestMethod]
        [TestCategory("Static Files")]
        public async Task StaticFilesShouldAlwaysProvideVirtualPathBasedOnAppVersion()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs
            {
                ActiveAppEnvironmentCustomizer =
                environment =>
                {
                    environment.AppInfo.Version = "2";
                }
            }))
            {
                HttpResponseMessage getVirtualPathUrlV1 = await testEnvironment.Server.BuildHttpClient()
                    .GetAsync("/Files/V1");

                Assert.AreNotEqual(HttpStatusCode.OK, getVirtualPathUrlV1.StatusCode);

                HttpResponseMessage getVirtualPathUrlV2 = await testEnvironment.Server.BuildHttpClient()
                    .GetAsync("/Files/V2");

                Assert.AreNotEqual(HttpStatusCode.NotFound, getVirtualPathUrlV2.StatusCode);
            }
        }

        [TestMethod]
        [TestCategory("Static Files"), TestCategory("ObjectsLifeCycle")]
        public async Task NoScopeShouldBeCreatedForStaticFilesMiddlewareRequests()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                await testEnvironment.Server.BuildHttpClient().GetAsync("/Files/V1");

                Assert.AreEqual(2 /* there's a test app startup which drops/creates db using TransactionAction */, testEnvironment.GetObjects<IScopeStatusManager>().Count());
            }
        }
    }
}
