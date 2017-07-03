using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Bit.Core.Models;
using Bit.Test;
using Bit.Test.Server;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;

namespace Bit.Tests.Api.Middlewares.Tests
{
    [TestClass]
    public class DefaultPageMiddlewareTests
    {
        [TestMethod]
        [TestCategory("DefaultPage"), TestCategory("Security")]
        public async Task ReturnDefaultPageForLoggedInUsersOnly()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                HttpResponseMessage getDefaultPageResponse = await testEnvironment.Server.GetHttpClient(token)
                    .GetAsync("/");

                Assert.AreEqual(HttpStatusCode.OK, getDefaultPageResponse.StatusCode);

                Assert.AreEqual("text/html", getDefaultPageResponse.Content.Headers.ContentType.MediaType);

                Assert.AreEqual("utf-8", getDefaultPageResponse.Content.Headers.ContentType.CharSet);
            }
        }

        [TestMethod]
        [TestCategory("DefaultPage"), TestCategory("Security")]
        public async Task DontReturnDefaultPageForNotLoggedInUsers()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                HttpResponseMessage getDefaultPageResponse = await testEnvironment.Server.GetHttpClient()
                    .GetAsync("/");

                Assert.AreNotEqual(HttpStatusCode.OK, getDefaultPageResponse.StatusCode);
            }
        }

        [TestMethod]
        [TestCategory("DefaultPage"), TestCategory("Caching")]
        public async Task DefaultPageResponseMustNotBeCacheable()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                HttpResponseMessage getDefaultPageResponse = await testEnvironment.Server.GetHttpClient(token)
                    .GetAsync("/");

                Assert.AreEqual(false, getDefaultPageResponse.Headers.CacheControl.Public);

                Assert.AreEqual(null, getDefaultPageResponse.Headers.CacheControl.MaxAge);

                Assert.AreEqual(true, getDefaultPageResponse.Headers.CacheControl.NoCache);

                Assert.AreEqual(true, getDefaultPageResponse.Headers.CacheControl.NoStore);

                Assert.AreEqual(true, getDefaultPageResponse.Headers.CacheControl.MustRevalidate);
            }
        }

        [TestMethod]
        [TestCategory("DefaultPage"), TestCategory("Security")]
        public async Task DefaultPageResponseMustHaveSecurityHeaders()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                HttpResponseMessage getDefaultPageResponse = await testEnvironment.Server.GetHttpClient(token)
                    .GetAsync("/");

                Assert.AreEqual(true, getDefaultPageResponse.Headers.Contains("X-Content-Type-Options"));
                Assert.AreEqual(true, getDefaultPageResponse.Headers.Contains("X-Download-Options"));
                Assert.AreEqual(true, getDefaultPageResponse.Headers.Contains("X-XSS-Protection"));
            }
        }

        [TestMethod]
        [TestCategory("DefaultPage"), TestCategory("Security")]
        public async Task DefaultPageResponseMustHaveStrictTransportSecurityHeadersInCaseOfSslRequired()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs
            {
                UseHttps = true,
                ActiveAppEnvironmentCustomizer = environment =>
                {
                    environment.AddOrReplace(new EnvironmentConfig { Key = "RequireSsl", Value = true });
                }
            }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                HttpResponseMessage getDefaultPageResponse = await testEnvironment.Server.GetHttpClient(token)
                    .GetAsync(testEnvironment.Server.Uri);

                Assert.AreEqual(true, getDefaultPageResponse.Headers.Contains("X-Content-Type-Options"));
                Assert.AreEqual(true, getDefaultPageResponse.Headers.Contains("X-Download-Options"));
                Assert.AreEqual(true, getDefaultPageResponse.Headers.Contains("X-XSS-Protection"));
                Assert.AreEqual(true, getDefaultPageResponse.Headers.Contains("Strict-Transport-Security"));
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("DefaultPage")]
        public virtual async Task TestDesiredEnvironmentsConfigsArePresentInClientSide()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest("testDesiredEnvironmentsConfigsArePresentInClientSide");
                }
            }
        }
    }
}