/*using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FakeItEasy;
using Foundation.Api.Exceptions;
using Foundation.Api.Metadata;
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
    public class ExceptionHandlerFilterAttributeTests
    {
        [Ignore]
        [TestMethod]
        [TestCategory("WebApi"), TestCategory("Logging")]
        public virtual async Task WebApiExceptionHandlerFilterAttributeMustReturnKnownErrorReasonPhraseAndInternalServerErrorStatusCodeAndCorrelationIdInResponseWhenAppExceptionThrownsInWebApi()
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
                TokenResponse token = testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

                ODataClient client = testEnvironment.Server.BuildODataClient(token: token, afterResponse: message =>
                {
                    if (message.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        Assert.AreEqual(FoundationMetadataBuilder.KnownError, message.ReasonPhrase);

                        ILogger logger = TestDependencyManager.CurrentTestDependencyManager.Objects
                            .OfType<ILogger>().Last();

                        Guid correlationId = (Guid)logger.LogData.Single(logData => logData.Key == "CorrelationId").Value;

                        Assert.AreEqual(correlationId.ToString(), message.Headers.Single(h => h.Key == "CorrelationId").Value.Single());
                    }
                });

                try
                {
                    await client.Controller<TestModelsController, TestModel>()
                        .Action(nameof(TestModelsController.SendEmail))
                        .Set(new { to = "Someone", title = "Email title", message = "Email message" })
                        .ExecuteAsync();

                    Assert.Fail();
                }
                catch (WebRequestException ex)
                {
                    Assert.IsTrue(ex.Response.Contains("Test"));

                    Assert.AreEqual(HttpStatusCode.InternalServerError, ex.Code);
                }
            }
        }

        [Ignore]
        [TestMethod]
        [TestCategory("WebApi"), TestCategory("Logging")]
        public virtual async Task WebApiExceptionHandlerFilterAttributeMustReturnKnownErrorReasonPhraseAndNotFoundStatusCodeAndCorrelationIdInResponseWhenResourceNotFoundExceptionThrownsInWebApi()
        {
            IEmailService emailService = A.Fake<IEmailService>();

            A.CallTo(() => emailService.SendEmail(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Throws(new ResourceNotFoundException("Test"));

            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs
            {
                AdditionalDependencies = manager =>
                {
                    manager.RegisterInstance(emailService);
                }
            }))
            {
                TokenResponse token = testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

                ODataClient client = testEnvironment.Server.BuildODataClient(token: token, afterResponse: message =>
                {
                    if (message.StatusCode == HttpStatusCode.NotFound)
                    {
                        Assert.AreEqual(FoundationMetadataBuilder.KnownError, message.ReasonPhrase);

                        ILogger logger = TestDependencyManager.CurrentTestDependencyManager.Objects
                            .OfType<ILogger>().Last();

                        Guid correlationId = (Guid)logger.LogData.Single(logData => logData.Key == "CorrelationId").Value;

                        Assert.AreEqual(correlationId.ToString(), message.Headers.Single(h => h.Key == "CorrelationId").Value.Single());
                    }
                });

                try
                {
                    await client.Controller<TestModelsController, TestModel>()
                        .Action(nameof(TestModelsController.SendEmail))
                        .Set(new { to = "Someone", title = "Email title", message = "Email message" })
                        .ExecuteAsync();

                    Assert.Fail();
                }
                catch (WebRequestException ex)
                {
                    Assert.IsTrue(ex.Response.Contains("Test"));

                    Assert.AreEqual(HttpStatusCode.NotFound, ex.Code);
                }
            }
        }

        [Ignore]
        [TestMethod]
        [TestCategory("WebApi"), TestCategory("Logging")]
        public virtual async Task WebApiExceptionHandlerFilterAttributeMustReturnUnKnownErrorReasonPhraseAndInternalServerErrorStatusCodeAndCorrelationIdInResponseWhenExceptionOtherThanAppExceptionIsThrown()
        {
            IEmailService emailService = A.Fake<IEmailService>();

            A.CallTo(() => emailService.SendEmail(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Throws(new InvalidOperationException("Test"));

            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs
            {
                AdditionalDependencies = manager =>
                {
                    manager.RegisterInstance(emailService);
                },
                ActiveAppEnvironmentCustomizer = appEnv =>
                {
                    appEnv.DebugMode = false;
                }
            }))
            {
                TokenResponse token = testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

                ODataClient client = testEnvironment.Server.BuildODataClient(token: token, afterResponse: message =>
                {
                    if (message.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        Assert.AreEqual(FoundationMetadataBuilder.UnKnownError, message.ReasonPhrase);

                        ILogger logger = TestDependencyManager.CurrentTestDependencyManager.Objects
                            .OfType<ILogger>().Last();

                        Guid correlationId = (Guid)logger.LogData.Single(logData => logData.Key == "CorrelationId").Value;

                        Assert.AreEqual(correlationId.ToString(), message.Headers.Single(h => h.Key == "CorrelationId").Value.Single());
                    }
                });

                try
                {
                    await client.Controller<TestModelsController, TestModel>()
                        .Action(nameof(TestModelsController.SendEmail))
                        .Set(new { to = "Someone", title = "Email title", message = "Email message" })
                        .ExecuteAsync();

                    Assert.Fail();
                }
                catch (WebRequestException ex)
                {
                    Assert.IsTrue(ex.Response.Contains(FoundationMetadataBuilder.UnKnownError));

                    Assert.AreEqual(HttpStatusCode.InternalServerError, ex.Code);
                }
            }
        }
    }
}
*/