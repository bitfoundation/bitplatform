using Bit.Core.Contracts;
using Bit.Core.Exceptions;
using Bit.Owin.Metadata;
using Bit.Test;
using Bit.Tests.Core.Contracts;
using FakeItEasy;
using Bit.Http.Contracts;
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
        public virtual async Task WebApiExceptionHandlerFilterAttributeMustReturnKnownErrorReasonPhraseAndInternalServerErrorStatusCodeAndXCorrelationIdInResponseWhenAppExceptionThrownsInWebApi()
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
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                IODataClient client = testEnvironment.BuildTestODataClient(token: token, odataClientSettings: new ODataClientSettings
                {
                    AfterResponse = message =>
                    {
                        if (message.StatusCode == HttpStatusCode.InternalServerError)
                        {
                            Assert.AreEqual(BitMetadataBuilder.KnownError, message.ReasonPhrase);
                            Assert.IsTrue(message.Headers.Any(h => h.Key == "Reason-Phrase" && h.Value.Any(v => v == BitMetadataBuilder.KnownError)));

                            ILogger logger = testEnvironment.GetObjects<ILogger>().Last();

                            string correlationId = (string)logger.LogData.Single(logData => logData.Key == "X-Correlation-ID").Value;

                            Assert.AreEqual(correlationId, message.Headers.Single(h => h.Key == "X-Correlation-ID").Value.Single());
                        }
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
                    Assert.IsTrue(ex.Response.Contains("Test"));

                    Assert.AreEqual(HttpStatusCode.InternalServerError, ex.Code);
                }
            }
        }

        [TestMethod]
        [TestCategory("WebApi"), TestCategory("Logging")]
        public virtual async Task WebApiExceptionHandlerFilterAttributeMustReturnKnownErrorReasonPhraseAndNotFoundStatusCodeAndXCorrelationIdInResponseWhenResourceNotFoundExceptionThrownsInWebApi()
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
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                IODataClient client = testEnvironment.BuildTestODataClient(token: token, odataClientSettings: new ODataClientSettings
                {
                    AfterResponse = message =>
                    {
                        if (message.StatusCode == HttpStatusCode.NotFound)
                        {
                            Assert.AreEqual(BitMetadataBuilder.KnownError, message.ReasonPhrase);
                            Assert.IsTrue(message.Headers.Any(h => h.Key == "Reason-Phrase" && h.Value.Any(v => v == BitMetadataBuilder.KnownError)));

                            ILogger logger = testEnvironment.GetObjects<ILogger>().Last();

                            string xCorrelationId = (string)logger.LogData.Single(logData => logData.Key == "X-Correlation-ID").Value;

                            Assert.AreEqual(xCorrelationId, message.Headers.Single(h => h.Key == "X-Correlation-ID").Value.Single());
                        }
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
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                IODataClient client = testEnvironment.BuildTestODataClient(token: token, odataClientSettings: new ODataClientSettings
                {
                    AfterResponse = message =>
                    {
                        if (message.StatusCode == HttpStatusCode.InternalServerError)
                        {
                            Assert.AreEqual(BitMetadataBuilder.UnknownError, message.ReasonPhrase);
                            Assert.IsTrue(message.Headers.Any(h => h.Key == "Reason-Phrase" && h.Value.Any(v => v == BitMetadataBuilder.UnknownError)));

                            ILogger logger = testEnvironment.GetObjects<ILogger>().Last();

                            string xCorrelationId = (string)logger.LogData.Single(logData => logData.Key == "X-Correlation-ID").Value;

                            Assert.AreEqual(xCorrelationId, message.Headers.Single(h => h.Key == "X-Correlation-ID").Value.Single());
                        }
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
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                HttpClient client = testEnvironment.Server.BuildHttpClient(token: token);

                HttpResponseMessage response = await client.DeleteAsync("odata/Test/parentEntities(3)"); // no route for this url!

                Assert.AreEqual("UnknownError:Not Found", response.ReasonPhrase);
            }
        }
    }
}