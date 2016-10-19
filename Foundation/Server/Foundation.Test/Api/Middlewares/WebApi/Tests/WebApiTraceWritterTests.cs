using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FakeItEasy;
using Foundation.Api.Exceptions;
using Foundation.Core.Contracts;
using Foundation.Test.Core.Contracts;
using Foundation.Test.Core.Implementations;
using Foundation.Test.Model.DomainModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Simple.OData.Client;
using Foundation.Test.Api.ApiControllers;

namespace Foundation.Test.Api.Middlewares.WebApi.Tests
{
    [TestClass]
    public class WebApiTraceWritterTests
    {
        [TestMethod]
        [TestCategory("WebApi"), TestCategory("Logging")]
        public virtual async Task WebApiTraceWritterShouldLogApiRequestIdAndExceptionDetails()
        {
            IEmailService emailService = A.Fake<IEmailService>();

            A.CallTo(() => emailService.SendEmail(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Throws(new AppException("Test"));

            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs
            {
                AdditionalDependencies = manager =>
                {
                    manager.RegisterInstance(emailService);
                }
            }))
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                ODataClient client = testEnvironment.Server.BuildODataClient(token: token);

                try
                {
                    await client.Controller<TestModelsController, TestModel>()
                        .Action(nameof(TestModelsController.SendEmail))
                        .Set(new { to = "Someone", title = "Email title", message = "Email message" })
                        .ExecuteAsync();

                    Assert.Fail();
                }
                catch (WebRequestException)
                {
                    ILogger logger = TestDependencyManager.CurrentTestDependencyManager.Objects
                            .OfType<ILogger>().Last();

                    Assert.IsTrue(logger.LogData.Single(ld => ld.Key == "ApiRequestId").Value is Guid);
                    Assert.AreEqual(typeof(AppException).GetTypeInfo().FullName, logger.LogData.Single(ld => ld.Key == "WebExceptionType").Value);
                    Assert.AreEqual("Test", ((AppException)logger.LogData.Single(ld => ld.Key == "WebException").Value).Message);
                }
            }
        }
    }
}
