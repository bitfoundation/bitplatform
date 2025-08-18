﻿using Bit.Http.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Bit.Tests.Api.Middlewares.WebApi.Tests
{
    [TestClass]
    public class WebApiODataSecurityTests
    {
        [TestMethod]
        [TestCategory("WebApi"), TestCategory("Security")]
        public async Task LoggedInUsersMustHaveAccessToODataWebApis()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword");

                HttpResponseMessage getMetadataResponse = await testEnvironment.Server.BuildHttpClient(token)
                        .GetAsync("/odata/Bit/$metadata");

                Assert.AreEqual(HttpStatusCode.OK, getMetadataResponse.StatusCode);
            }
        }

        [TestMethod]
        [TestCategory("WebApi"), TestCategory("Security")]
        public async Task WebApiResponsesMustHaveSecurityHeaders()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword");

                HttpResponseMessage getMetadataResponse = await testEnvironment.Server.BuildHttpClient(token)
                        .GetAsync("/odata/Bit/$metadata");

                Assert.AreEqual(true, getMetadataResponse.Headers.Contains("X-Content-Type-Options"));
            }
        }

        [TestMethod]
        [TestCategory("WebApi"), TestCategory("Security")]
        public async Task NotLoggedInUsersMustHaveAccessToMetadataAnyway()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                HttpResponseMessage getMetadataResponse = await testEnvironment.Server.BuildHttpClient()
                        .GetAsync("/odata/Test/$metadata");

                Assert.AreEqual(HttpStatusCode.OK, getMetadataResponse.StatusCode);
            }
        }

        [TestMethod]
        [TestCategory("WebApi"), TestCategory("Security")]
        public async Task NotLoggedInUsersMustNotHaveAccessToProtectedResources()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                HttpResponseMessage getMetadataResponse = await testEnvironment.Server.BuildHttpClient()
                        .GetAsync("/odata/Test/ValidationSamples/");

                Assert.AreEqual(HttpStatusCode.Unauthorized, getMetadataResponse.StatusCode);
            }
        }

        [TestMethod]
        [TestCategory("WebApi"), TestCategory("Security")]
        public async Task NotLoggedInUsersMustHaveAccessToUnProtectedWebApis()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                HttpResponseMessage getTestModels = await testEnvironment.Server.BuildHttpClient()
                        .GetAsync("/odata/Test/TestModels");

                Assert.AreEqual(HttpStatusCode.OK, getTestModels.StatusCode);
            }
        }
    }
}
