using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Foundation.Api.Contracts.Metadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Foundation.Test.Api.Middlewares.Tests
{
    [TestClass]
    public class MetadataMiddlewareTests
    {
        [TestMethod]
        [TestCategory("Metadata")]
        public async Task MetadataMiddlewareShouldAlwaysProvideMetadataBasedOnAppVersion()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs
            {
                ActiveAppEnvironmentCustomizer =
                environment =>
                {
                    environment.AppInfo.Version = "2";
                }
            }))
            {
                HttpResponseMessage getMetadataForV1 = await testEnvironment.Server.GetHttpClient()
                    .GetAsync("Metadata/V1");

                Assert.AreNotEqual(HttpStatusCode.OK, getMetadataForV1.StatusCode);

                HttpResponseMessage getMetadataForV2 = await testEnvironment.Server.GetHttpClient()
                    .GetAsync("Metadata/V2");

                Assert.AreEqual(HttpStatusCode.OK, getMetadataForV2.StatusCode);
            }
        }

        [TestMethod]
        [TestCategory("Metadata")]
        public async Task MetadataMiddlewareResponseMustBeDeSerializeableForSimpleCSharpClient()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment())
            {
                HttpResponseMessage getMetadataForV1 = await testEnvironment.Server.GetHttpClient()
                    .GetAsync("Metadata/V1");

                getMetadataForV1.EnsureSuccessStatusCode();

                AppMetadata appMetadata = await getMetadataForV1.Content.ReadAsAsync<AppMetadata>();

                Assert.IsTrue(appMetadata.Messages.Any());

                Assert.IsTrue(appMetadata.Dtos.Any());

                Assert.IsTrue(appMetadata.Projects.Any());
            }
        }

        [TestMethod]
        [TestCategory("Metadata"), TestCategory("Caching")]
        public async Task MetadataMiddlewareResultMustBeCacheable()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment())
            {
                HttpResponseMessage getMetadataForV1 = await testEnvironment.Server.GetHttpClient()
                    .GetAsync("Metadata/V1");

                Assert.AreEqual(true, getMetadataForV1.Headers.CacheControl.Public);

                Assert.AreEqual(TimeSpan.FromDays(365), getMetadataForV1.Headers.CacheControl.MaxAge);

                Assert.AreEqual(false, getMetadataForV1.Headers.CacheControl.NoCache);

                Assert.AreEqual(false, getMetadataForV1.Headers.CacheControl.NoStore);

                Assert.AreEqual(false, getMetadataForV1.Headers.CacheControl.MustRevalidate);
            }
        }

        [TestMethod]
        [TestCategory("Metadata"), TestCategory("Security")]
        public async Task MetadataMiddlewareResultMustHaveSecurityHeaders()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment())
            {
                HttpResponseMessage getMetadataForV1 = await testEnvironment.Server.GetHttpClient()
                    .GetAsync("Metadata/V1");

                Assert.AreEqual(true, getMetadataForV1.Headers.Contains("X-Content-Type-Options"));
            }
        }

        [TestMethod]
        [TestCategory("Metadata")]
        public async Task MetadataMiddlewareResultMustBeJson()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment())
            {
                HttpResponseMessage getMetadataForV1 = await testEnvironment.Server.GetHttpClient()
                    .GetAsync("Metadata/V1");

                Assert.AreEqual("application/json", getMetadataForV1.Content.Headers.ContentType.MediaType);

                Assert.AreEqual("utf-8", getMetadataForV1.Content.Headers.ContentType.CharSet);
            }
        }
    }
}
