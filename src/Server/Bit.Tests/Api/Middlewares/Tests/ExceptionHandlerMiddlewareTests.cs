using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Test;
using Bit.Test.Implementations;
using FakeItEasy;
using Bit.Http.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Owin;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Bit.Tests.Api.Middlewares.Tests
{
    [TestClass]
    public class ExceptionHandlerMiddlewareTests
    {
        [TestMethod]
        [TestCategory("ExceptionHandler"), TestCategory("Logging")]
        public async Task ExceptionHandlerMustNotSaveAnyThingToLogStoreBecauseOfSuccessfulRequests()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                IScopeStatusManager scopeStatusManager = testEnvironment.GetObjects<IScopeStatusManager>()
                    .Last();

                A.CallTo(() => scopeStatusManager.MarkAsSucceeded())
                    .MustHaveHappenedOnceExactly();
            }
        }

        [TestMethod]
        [TestCategory("ExceptionHandler"), TestCategory("Logging")]
        public async Task ExceptionHandlerMustSaveExceptionToLogStoreBecauseOfExceptionInRequest()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs
            {
                AdditionalDependencies = (manager, services) =>
                {
                    manager.RegisterOwinMiddlewareUsing(owinApp =>
                    {
                        owinApp.Map("/Exception", innerApp =>
                        {
                            innerApp.Use<ExceptionThrownMiddleware>();
                        });
                    });
                }
            }))
            {
                try
                {
                    Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                    await testEnvironment.Server.BuildHttpClient(token)
                        .GetAsync("/Exception");

                    Assert.Fail();
                }
                catch
                {
                    IScopeStatusManager scopeStatusManager = testEnvironment.GetObjects<IScopeStatusManager>()
                        .Last();

                    A.CallTo(() => scopeStatusManager.MarkAsFailed("Operation is not valid due to the current state of the object."))
                        .MustHaveHappenedOnceExactly();

                    ILogger logger = testEnvironment.GetObjects<ILogger>()
                        .Last();

                    A.CallTo(() => logger.LogExceptionAsync(A<Exception>.That.Matches(e => e is InvalidOperationException), A<string>.Ignored))
                        .MustHaveHappenedOnceExactly();

                    LogData[] logData = logger.LogData.ToArray();

                    logData.Single(c => c.Key == "ExceptionType" && ((string)c.Value).Contains("InvalidOperationException"));
                    logData.Single(c => c.Key == nameof(IRequestInformationProvider.HttpMethod) && (string)c.Value == "GET");
                    logData.Single(c => c.Key == "UserId" && (string)c.Value == "ValidUserName");
                }
            }
        }

        [TestMethod]
        [TestCategory("ExceptionHandler"), TestCategory("Logging")]
        public async Task ExceptionHandlerMustSaveFatalToLogStoreBecauseOfErrorRelatedToServer()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs
            {
                AdditionalDependencies = (manager, services) =>
                {
                    manager.RegisterOwinMiddlewareUsing(owinApp =>
                    {
                        owinApp.Map("/InternalServerError", innerApp =>
                        {
                            innerApp.Use<InternalServerErrorMiddleware>();
                        });
                    });
                }
            }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                await testEnvironment.Server.BuildHttpClient(token)
                            .GetAsync("/InternalServerError");

                IScopeStatusManager scopeStatusManager = testEnvironment.GetObjects<IScopeStatusManager>()
                    .Last();

                A.CallTo(() => scopeStatusManager.MarkAsFailed("UnknownError"))
                            .MustHaveHappenedOnceExactly();

                ILogger logger = testEnvironment.GetObjects<ILogger>()
                    .Last();

                A.CallTo(() => logger.LogFatalAsync(A<string>.Ignored))
                            .MustHaveHappenedOnceExactly();

                LogData[] logData = logger.LogData.ToArray();

                logData.Single(c => c.Key == "ResponseStatusCode" && (int)c.Value == 501);
                logData.Single(c => c.Key == nameof(IRequestInformationProvider.HttpMethod) && (string)c.Value == "GET");
                logData.Single(c => c.Key == "UserId" && (string)c.Value == "ValidUserName");
            }
        }

        [TestMethod]
        [TestCategory("ExceptionHandler"), TestCategory("Logging")]
        public async Task ExceptionHandlerMustSaveWarningToLogStoreBecauseOfErrorRelatedToClientRequest()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                await testEnvironment.Server.BuildHttpClient(token)
                    .GetAsync("/odata/Test/XYZ");

                IScopeStatusManager scopeStatusManager = testEnvironment.GetObjects<IScopeStatusManager>()
                    .Last();

                A.CallTo(() => scopeStatusManager.MarkAsFailed("Not Acceptable"))
                    .MustHaveHappenedOnceExactly();

                ILogger logger = TestDependencyManager.CurrentTestDependencyManager
                    .Objects.OfType<ILogger>()
                    .Last();

                A.CallTo(() => logger.LogWarningAsync(A<string>.Ignored))
                    .MustHaveHappenedOnceExactly();

                LogData[] logData = logger.LogData.ToArray();

                logData.Single(c => c.Key == "ResponseStatusCode" && (int)c.Value == 406);
                logData.Single(c => c.Key == nameof(IRequestInformationProvider.HttpMethod) && (string)c.Value == "GET");
                logData.Single(c => c.Key == "UserId" && (string)c.Value == "ValidUserName");
            }
        }
    }
}
