using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Owin.Exceptions;
using Bit.Owin.Metadata;
using Bit.Test;
using Bit.Test.Core.Implementations;
using Bit.Tests.Api.ApiControllers;
using Bit.Tests.Core.Contracts;
using Bit.Tests.Model.DomainModels;
using FakeItEasy;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Simple.OData.Client;

namespace Bit.Tests.Api.Middlewares.WebApi.Tests
{
    [TestClass]
    public class ExceptionHandlerFilterAttributeTests
    {
        [TestMethod]
        [TestCategory("WebApi"), TestCategory("Logging")]
        public virtual async Task WebApiExceptionHandlerFilterAttributeMustReturnKnownErrorReasonPhraseAndInternalServerErrorStatusCodeAndCorrelationIdInResponseWhenAppExceptionThrownsInWebApi()
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

                ODataClient client = testEnvironment.Server.BuildODataClient(token: token, afterResponse: message =>
                {
                    if (message.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        Assert.AreEqual(BitMetadataBuilder.KnownError, message.ReasonPhrase);

                        ILogger logger = TestDependencyManager.CurrentTestDependencyManager.Objects
                            .OfType<ILogger>().Last();

                        Guid correlationId = (Guid)logger.LogData.Single(logData => logData.Key == "X-CorrelationId").Value;

                        Assert.AreEqual(correlationId.ToString(), message.Headers.Single(h => h.Key == "X-CorrelationId").Value.Single());
                    }
                });

                try
                {
                    await client.Controller<TestModelsController, TestModel>()
                        .Action(nameof(TestModelsController.SendEmail))
                        .Set(new TestModelsController.EmailParameters { to = "Someone", title = "Email title", message = "Email message" })
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

        [TestMethod]
        [TestCategory("WebApi"), TestCategory("Logging")]
        public virtual async Task WebApiExceptionHandlerFilterAttributeMustReturnKnownErrorReasonPhraseAndNotFoundStatusCodeAndCorrelationIdInResponseWhenResourceNotFoundExceptionThrownsInWebApi()
        {
            IEmailService emailService = A.Fake<IEmailService>();

            A.CallTo(() => emailService.SendEmail(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Throws(new ResourceNotFoundException("Test"));

            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs
            {
                AdditionalDependencies = manager =>
                {
                    manager.RegisterInstance(emailService);
                }
            }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

                ODataClient client = testEnvironment.Server.BuildODataClient(token: token, afterResponse: message =>
                {
                    if (message.StatusCode == HttpStatusCode.NotFound)
                    {
                        Assert.AreEqual(BitMetadataBuilder.KnownError, message.ReasonPhrase);

                        ILogger logger = TestDependencyManager.CurrentTestDependencyManager.Objects
                            .OfType<ILogger>().Last();

                        Guid correlationId = (Guid)logger.LogData.Single(logData => logData.Key == "X-CorrelationId").Value;

                        Assert.AreEqual(correlationId.ToString(), message.Headers.Single(h => h.Key == "X-CorrelationId").Value.Single());
                    }
                });

                try
                {
                    await client.Controller<TestModelsController, TestModel>()
                        .Action(nameof(TestModelsController.SendEmail))
                        .Set(new TestModelsController.EmailParameters { to = "Someone", title = "Email title", message = "Email message" })
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

        [TestMethod]
        [TestCategory("WebApi"), TestCategory("Logging")]
        public virtual async Task WebApiExceptionHandlerFilterAttributeMustReturnUnKnownErrorReasonPhraseAndInternalServerErrorStatusCodeAndCorrelationIdInResponseWhenExceptionOtherThanAppExceptionIsThrown()
        {
            IEmailService emailService = A.Fake<IEmailService>();

            A.CallTo(() => emailService.SendEmail(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Throws(new InvalidOperationException("Test"));

            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs
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
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

                ODataClient client = testEnvironment.Server.BuildODataClient(token: token, afterResponse: message =>
                {
                    if (message.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        Assert.AreEqual(BitMetadataBuilder.UnKnownError, message.ReasonPhrase);

                        ILogger logger = TestDependencyManager.CurrentTestDependencyManager.Objects
                            .OfType<ILogger>().Last();

                        Guid correlationId = (Guid)logger.LogData.Single(logData => logData.Key == "X-CorrelationId").Value;

                        Assert.AreEqual(correlationId.ToString(), message.Headers.Single(h => h.Key == "X-CorrelationId").Value.Single());
                    }
                });

                try
                {
                    await client.Controller<TestModelsController, TestModel>()
                        .Action(nameof(TestModelsController.SendEmail))
                        .Set(new TestModelsController.EmailParameters { to = "Someone", title = "Email title", message = "Email message" })
                        .ExecuteAsync();

                    Assert.Fail();
                }
                catch (WebRequestException ex)
                {
                    Assert.IsTrue(ex.Response.Contains(BitMetadataBuilder.UnKnownError));

                    Assert.AreEqual(HttpStatusCode.InternalServerError, ex.Code);
                }
            }
        }
    }
}