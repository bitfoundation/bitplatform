using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bit.Owin.Exceptions;
using Bit.Core.Contracts;
using Bit.Test.Core.Implementations;
using Simple.OData.Client;
using Bit.Tests.Api.ApiControllers;
using Bit.Tests.Model.DomainModels;
using Bit.Tests.Core.Contracts;
using FakeItEasy;
using Bit.Test;
using System.Reflection;

namespace Bit.Tests.Api.Middlewares.WebApi.Tests
{
    [TestClass]
    public class WebApiTraceWritterTests
    {
        [TestMethod]
        [TestCategory("WebApi"), TestCategory("Logging")]
        public virtual async Task WebApiShouldReturnTheSameCorrelationIdInResponses()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

                HttpClient client = testEnvironment.Server.GetHttpClient(token);

                Guid correlationId = Guid.NewGuid();

                client.DefaultRequestHeaders.Add("X-CorrelationId", correlationId.ToString());

                HttpResponseMessage response = await client.GetAsync("/odata/Test/$metadata");

                Assert.AreEqual(correlationId, Guid.Parse(response.Headers.GetValues("X-CorrelationId").Single()));
            }
        }

        [TestMethod]
        [TestCategory("WebApi"), TestCategory("Logging")]
        public virtual async Task WebApiTraceWritterShouldLogCorrelationIdAndExceptionDetails()
        {
            IEmailService emailService = A.Fake<IEmailService>();

            A.CallTo(() => emailService.SendEmail(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Throws(new AppException("Test"));

            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs
            {
                AdditionalDependencies = manager =>
                {
                    manager.RegisterInstance(emailService);
                }
            }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

                ODataClient client = testEnvironment.Server.BuildODataClient(token: token);

                try
                {
                    await client.Controller<TestModelsController, TestModel>()
                        .Action(nameof(TestModelsController.SendEmail))
                        .Set(new TestModelsController.EmailParameters { to = "Someone", title = "Email title", message = "Email message" })
                        .ExecuteAsync();

                    Assert.Fail();
                }
                catch (WebRequestException)
                {
                    ILogger logger = TestDependencyManager.CurrentTestDependencyManager.Objects
                            .OfType<ILogger>().Last();

                    Assert.IsTrue(logger.LogData.Single(ld => ld.Key == "X-CorrelationId").Value is Guid);
                    Assert.AreEqual(typeof(AppException).GetTypeInfo().FullName, logger.LogData.Single(ld => ld.Key == "WebExceptionType").Value);
                    Assert.AreEqual("Test", ((AppException)logger.LogData.Single(ld => ld.Key == "WebException").Value).Message);
                }
            }
        }
    }
}
