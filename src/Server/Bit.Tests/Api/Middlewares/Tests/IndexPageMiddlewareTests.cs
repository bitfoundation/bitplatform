using Bit.Core.Models;
using Bit.Test;
using Bit.Test.Server;
using Bit.Http.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Bit.Tests.Api.Middlewares.Tests
{
    [TestClass]
    public class IndexPageMiddlewareTests
    {
        [TestMethod]
        [TestCategory("IndexPage"), TestCategory("Security")]
        public async Task ReturnIndexPageForLoggedInUsersOnly()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                HttpResponseMessage getIndexPageResponse = await testEnvironment.Server.BuildHttpClient(token)
                    .GetAsync("/");

                Assert.AreEqual(HttpStatusCode.OK, getIndexPageResponse.StatusCode);

                Assert.AreEqual("text/html", getIndexPageResponse.Content.Headers.ContentType.MediaType);

                Assert.AreEqual("utf-8", getIndexPageResponse.Content.Headers.ContentType.CharSet);
            }
        }

        [TestMethod]
        [TestCategory("IndexPage"), TestCategory("Security")]
        public async Task DontReturnIndexPageForNotLoggedInUsers()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                HttpResponseMessage getIndexPageResponse = await testEnvironment.Server.BuildHttpClient()
                    .GetAsync("/");

                Assert.AreNotEqual(HttpStatusCode.OK, getIndexPageResponse.StatusCode);
            }
        }

        [TestMethod]
        [TestCategory("IndexPage"), TestCategory("Caching")]
        public async Task IndexPageResponseMustNotBeCacheable()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                HttpResponseMessage getIndexPageResponse = await testEnvironment.Server.BuildHttpClient(token)
                    .GetAsync("/");

                Assert.AreEqual(false, getIndexPageResponse.Headers.CacheControl.Public);

                Assert.AreEqual(null, getIndexPageResponse.Headers.CacheControl.MaxAge);

                Assert.AreEqual(true, getIndexPageResponse.Headers.CacheControl.NoStore);
            }
        }

        [TestMethod]
        [TestCategory("IndexPage"), TestCategory("Security")]
        public async Task IndexPageResponseMustHaveSecurityHeaders()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                HttpResponseMessage getIndexPageResponse = await testEnvironment.Server.BuildHttpClient(token)
                    .GetAsync("/");

                Assert.AreEqual(true, getIndexPageResponse.Headers.Contains("X-Content-Type-Options"));
                Assert.AreEqual(true, getIndexPageResponse.Headers.Contains("X-Download-Options"));
                Assert.AreEqual(true, getIndexPageResponse.Headers.Contains("X-XSS-Protection"));
            }
        }

        [TestMethod]
        [TestCategory("IndexPage"), TestCategory("Security")]
        public async Task IndexPageResponseMustHaveStrictTransportSecurityHeadersInCaseOfSslRequired()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs
            {
                UseHttps = true,
                ActiveAppEnvironmentCustomizer = environment =>
                {
                    environment.AddOrReplace(new EnvironmentConfig { Key = AppEnvironment.KeyValues.RequireSsl, Value = true });
                }
            }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                HttpResponseMessage getIndexPageResponse = await testEnvironment.Server.BuildHttpClient(token)
                    .GetAsync(testEnvironment.Server.Uri);

                Assert.AreEqual(true, getIndexPageResponse.Headers.Contains("X-Content-Type-Options"));
                Assert.AreEqual(true, getIndexPageResponse.Headers.Contains("X-Download-Options"));
                Assert.AreEqual(true, getIndexPageResponse.Headers.Contains("X-XSS-Protection"));
                Assert.AreEqual(true, getIndexPageResponse.Headers.Contains("Strict-Transport-Security"));
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("IndexPage")]
        public virtual async Task TestDesiredEnvironmentsConfigsArePresentInClientSide()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (WebDriver driver = testEnvironment.Server.BuildWebDriver(new WebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest("testDesiredEnvironmentsConfigsArePresentInClientSide");
                }
            }
        }
    }
}