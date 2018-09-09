using Bit.Owin.Contracts.Metadata;
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
    public class MetadataMiddlewareTests
    {
        [TestMethod]
        [TestCategory("Metadata")]
        public async Task MetadataMiddlewareShouldAlwaysProvideMetadataBasedOnAppVersion()
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
                HttpResponseMessage getMetadataForV1 = await testEnvironment.Server.BuildHttpClient()
                    .GetAsync("Metadata/V1");

                Assert.AreNotEqual(HttpStatusCode.OK, getMetadataForV1.StatusCode);

                HttpResponseMessage getMetadataForV2 = await testEnvironment.Server.BuildHttpClient()
                    .GetAsync("Metadata/V2");

                Assert.AreEqual(HttpStatusCode.OK, getMetadataForV2.StatusCode);
            }
        }

        [TestMethod]
        [TestCategory("Metadata")]
        public async Task MetadataMiddlewareResponseMustBeDeSerializableForSimpleCSharpClient()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                HttpResponseMessage getMetadataForV1 = await testEnvironment.Server.BuildHttpClient()
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
        public async Task MetadataMiddlewareResultMustBeCacheableInNonDebugMode()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs
            {
                ActiveAppEnvironmentCustomizer = appEnvironment => appEnvironment.DebugMode = false
            }))
            {
                HttpResponseMessage getMetadataForV1 = await testEnvironment.Server.BuildHttpClient()
                    .GetAsync("Metadata/V1");

                Assert.AreEqual(true, getMetadataForV1.Headers.CacheControl.Public);

                Assert.AreEqual(TimeSpan.FromDays(365), getMetadataForV1.Headers.CacheControl.MaxAge);

                Assert.AreEqual(false, getMetadataForV1.Headers.CacheControl.NoCache);

                Assert.AreEqual(false, getMetadataForV1.Headers.CacheControl.NoStore);

                Assert.AreEqual(false, getMetadataForV1.Headers.CacheControl.MustRevalidate);
            }
        }

        [TestMethod]
        [TestCategory("Metadata"), TestCategory("Caching")]
        public async Task MetadataMiddlewareResultMustNotBeCacheableInDebugMode()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                HttpResponseMessage getMetadataForV1 = await testEnvironment.Server.BuildHttpClient()
                    .GetAsync("Metadata/V1");

                Assert.AreEqual(false, getMetadataForV1.Headers.CacheControl.Public);

                Assert.AreEqual(null, getMetadataForV1.Headers.CacheControl.MaxAge);

                Assert.AreEqual(true, getMetadataForV1.Headers.CacheControl.NoCache);

                Assert.AreEqual(true, getMetadataForV1.Headers.CacheControl.NoStore);

                Assert.AreEqual(true, getMetadataForV1.Headers.CacheControl.MustRevalidate);
            }
        }

        [TestMethod]
        [TestCategory("Metadata"), TestCategory("Security")]
        public async Task MetadataMiddlewareResultMustHaveSecurityHeaders()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                HttpResponseMessage getMetadataForV1 = await testEnvironment.Server.BuildHttpClient()
                    .GetAsync("Metadata/V1");

                Assert.AreEqual(true, getMetadataForV1.Headers.Contains("X-Content-Type-Options"));
            }
        }

        [TestMethod]
        [TestCategory("Metadata")]
        public async Task MetadataMiddlewareResultMustBeJson()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                HttpResponseMessage getMetadataForV1 = await testEnvironment.Server.BuildHttpClient()
                    .GetAsync("Metadata/V1");

                Assert.AreEqual("application/json", getMetadataForV1.Content.Headers.ContentType.MediaType);

                Assert.AreEqual("utf-8", getMetadataForV1.Content.Headers.ContentType.CharSet);
            }
        }
    }
}
