using Bit.Core.Contracts;
using Bit.Core.Exceptions;
using Bit.Core.Models;
using Bit.Test;
using Bit.Tests.Core.Contracts;
using FakeItEasy;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Simple.OData.Client;
using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

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
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                HttpClient client = testEnvironment.Server.BuildHttpClient(token);

                Guid xCorrelationId = Guid.NewGuid();

                client.DefaultRequestHeaders.Add("X-Correlation-ID", xCorrelationId.ToString());

                HttpResponseMessage response = await client.GetAsync("/odata/Test/$metadata");

                Assert.AreEqual(xCorrelationId, Guid.Parse(response.Headers.GetValues("X-Correlation-ID").Single()));
            }
        }

        [TestMethod]
        [TestCategory("WebApi"), TestCategory("Logging")]
        public virtual async Task WebApiTraceWriterShouldLogXCorrelationIdAndExceptionDetails()
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

                IODataClient client = testEnvironment.Server.BuildODataClient(token: token);

                try
                {
                    await client.TestModels()
                        .SendEmail(to: "Someone", title: "Email title", message: "Email message")
                        .ExecuteAsync();

                    Assert.Fail();
                }
                catch (WebRequestException)
                {
                    ILogger logger = testEnvironment.GetObjects<ILogger>().Last();

                    Assert.IsTrue(logger.LogData.Single(ld => ld.Key == "X-Correlation-ID").Value is string);
                    Assert.AreEqual(typeof(AppException).GetTypeInfo().FullName, logger.LogData.Single(ld => ld.Key == "WebExceptionType").Value);
                    Assert.IsTrue(((string)(logger.LogData.Single(ld => ld.Key == "WebException").Value)).Contains("Bit.Owin.Exceptions.AppException: Test"));
                }
            }
        }

        public class FakeLogStore : ILogStore
        {
            public static int LogEntriesCount = 0;

            public void SaveLog(LogEntry logEntry)
            {
                if (logEntry.LogData.Any(ld => ld.Key == "Test"))
                    LogEntriesCount++;
            }

            public Task SaveLogAsync(LogEntry logEntry)
            {
                if (logEntry.LogData.Any(ld => ld.Key == "Test"))
                    LogEntriesCount++;

                return Task.CompletedTask;
            }
        }

        [TestMethod]
        [TestCategory("WebApi"), TestCategory("Logging")]
        public virtual async Task RequestShouldGetsLoggedAnywayIfLoggerPolicyDemandsIt()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs
            {
                AdditionalDependencies = (dependencyManager, services) =>
                {
                    dependencyManager.RegisterLogStore<FakeLogStore>();
                }
            }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                HttpClient client = testEnvironment.Server.BuildHttpClient(token: token);

                await client.GetAsync("api/actions/some-action/log=true");
                await client.GetAsync("api/actions/some-action/log=false");

                Assert.AreEqual(1, FakeLogStore.LogEntriesCount);
            }
        }
    }
}
