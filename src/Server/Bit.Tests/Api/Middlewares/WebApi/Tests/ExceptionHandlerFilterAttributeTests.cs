using Bit.Core.Contracts;
using Bit.Owin.Exceptions;
using Bit.Owin.Metadata;
using Bit.Test;
using Bit.Test.Implementations;
using Bit.Tests.Api.ApiControllers;
using Bit.Tests.Core.Contracts;
using Bit.Tests.Model.DomainModels;
using FakeItEasy;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Simple.OData.Client;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

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
                AdditionalDependencies = (manager, services) =>
                {
                    manager.RegisterInstance(emailService);
                }
            }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                IODataClient client = testEnvironment.Server.BuildODataClient(token: token, afterResponse: message =>
                {
                    if (message.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        Assert.AreEqual(BitMetadataBuilder.KnownError, message.ReasonPhrase);
                        Assert.IsTrue(message.Headers.Any(h => h.Key == "Reason-Phrase" && h.Value.Any(v => v == BitMetadataBuilder.KnownError)));

                        ILogger logger = TestDependencyManager.CurrentTestDependencyManager.Objects
                            .OfType<ILogger>().Last();

                        Guid correlationId = (Guid)logger.LogData.Single(logData => logData.Key == "X-CorrelationId").Value;

                        Assert.AreEqual(correlationId.ToString(), message.Headers.Single(h => h.Key == "X-CorrelationId").Value.Single());
                    }
                });

                try
                {
                    await client.TestModels()
                        .SendEmail(to : "Someone", title : "Email title", message : "Email message")
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
                AdditionalDependencies = (manager, services) =>
                {
                    manager.RegisterInstance(emailService);
                }
            }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                IODataClient client = testEnvironment.Server.BuildODataClient(token: token, afterResponse: message =>
                {
                    if (message.StatusCode == HttpStatusCode.NotFound)
                    {
                        Assert.AreEqual(BitMetadataBuilder.KnownError, message.ReasonPhrase);
                        Assert.IsTrue(message.Headers.Any(h => h.Key == "Reason-Phrase" && h.Value.Any(v => v == BitMetadataBuilder.KnownError)));

                        ILogger logger = TestDependencyManager.CurrentTestDependencyManager.Objects
                            .OfType<ILogger>().Last();

                        Guid correlationId = (Guid)logger.LogData.Single(logData => logData.Key == "X-CorrelationId").Value;

                        Assert.AreEqual(correlationId.ToString(), message.Headers.Single(h => h.Key == "X-CorrelationId").Value.Single());
                    }
                });

                try
                {
                    await client.TestModels()
                        .SendEmail(to : "Someone", title : "Email title", message : "Email message")
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
        public virtual async Task WebApiExceptionHandlerFilterAttributeMustReturnUnknownErrorReasonPhraseAndInternalServerErrorStatusCodeAndCorrelationIdInResponseWhenExceptionOtherThanAppExceptionIsThrown()
        {
            IEmailService emailService = A.Fake<IEmailService>();

            A.CallTo(() => emailService.SendEmail(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Throws(new InvalidOperationException("Test"));

            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs
            {
                AdditionalDependencies = (manager, services) =>
                {
                    manager.RegisterInstance(emailService);
                },
                ActiveAppEnvironmentCustomizer = appEnv =>
                {
                    appEnv.DebugMode = false;
                }
            }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                IODataClient client = testEnvironment.Server.BuildODataClient(token: token, afterResponse: message =>
                {
                    if (message.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        Assert.AreEqual(BitMetadataBuilder.UnknownError, message.ReasonPhrase);
                        Assert.IsTrue(message.Headers.Any(h => h.Key == "Reason-Phrase" && h.Value.Any(v => v == BitMetadataBuilder.UnknownError)));

                        ILogger logger = TestDependencyManager.CurrentTestDependencyManager.Objects
                            .OfType<ILogger>().Last();

                        Guid correlationId = (Guid)logger.LogData.Single(logData => logData.Key == "X-CorrelationId").Value;

                        Assert.AreEqual(correlationId.ToString(), message.Headers.Single(h => h.Key == "X-CorrelationId").Value.Single());
                    }
                });

                try
                {
                    await client.TestModels()
                        .SendEmail(to: "Someone", title: "Email title", message: "Email message")
                        .ExecuteAsync();

                    Assert.Fail();
                }
                catch (WebRequestException ex)
                {
                    Assert.IsTrue(ex.Response.Contains(BitMetadataBuilder.UnknownError));

                    Assert.AreEqual(HttpStatusCode.InternalServerError, ex.Code);
                }
            }
        }

        [TestMethod]
        [TestCategory("WebApi"), TestCategory("Logging")]
        public virtual async Task WebApiRoutingNotFoundReasonPhraseMustGetsReturnedAsUnknownError_NotFound()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                HttpClient client = testEnvironment.Server.BuildHttpClient(token: token);

                HttpResponseMessage response = await client.DeleteAsync("odata/Test/parentEntities(3)"); // no route for this url!

                Assert.AreEqual("UnknownError:Not Found", response.ReasonPhrase);
            }
        }
    }
}