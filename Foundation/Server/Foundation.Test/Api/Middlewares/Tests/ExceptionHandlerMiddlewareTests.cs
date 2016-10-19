using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using Foundation.Api.Contracts;
using Foundation.Core.Contracts;
using Foundation.Core.Models;
using Foundation.Test.Core.Implementations;
using Owin;

namespace Foundation.Test.Api.Middlewares.Tests
{
    [TestClass]
    public class ExceptionHandlerMiddlewareTests
    {
        [TestMethod]
        [TestCategory("ExceptionHandler"), TestCategory("Logging")]
        public async void ExceptionHandlerMustNotSaveAnyThingToLogStoreBecauseOfSuccessfulRequests()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment())
            {
                testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                IScopeStatusManager scopeStatusManager = TestDependencyManager.CurrentTestDependencyManager
                    .Objects.OfType<IScopeStatusManager>()
                    .Single();

                A.CallTo(() => scopeStatusManager.MarkAsSucceeded())
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }

        [TestMethod]
        [TestCategory("ExceptionHandler"), TestCategory("Logging")]
        public async Task ExceptionHandlerMustSaveExceptionToLogStoreBecauseOfExceptionInRequest()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs
            {
                AdditionalDependencies = manager =>
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
                    OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                    await testEnvironment.Server.GetHttpClient(token)
                        .GetAsync("/Exception");

                    Assert.Fail();
                }
                catch
                {
                    IScopeStatusManager scopeStatusManager = TestDependencyManager.CurrentTestDependencyManager
                        .Objects.OfType<IScopeStatusManager>()
                        .Last();

                    A.CallTo(() => scopeStatusManager.MarkAsFailed())
                        .MustHaveHappened(Repeated.Exactly.Once);

                    ILogger logger = TestDependencyManager.CurrentTestDependencyManager
                        .Objects.OfType<ILogger>()
                        .Last();

                    A.CallTo(() => logger.LogExceptionAsync(A<Exception>.That.Matches(e => e is InvalidOperationException), A<string>.Ignored))
                        .MustHaveHappened(Repeated.Exactly.Once);

                    IEnumerable<LogData> logData = logger.LogData;

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
            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs
            {
                AdditionalDependencies = manager =>
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
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                await testEnvironment.Server.GetHttpClient(token)
                            .GetAsync("/InternalServerError");

                IScopeStatusManager scopeStatusManager = TestDependencyManager.CurrentTestDependencyManager
                    .Objects.OfType<IScopeStatusManager>()
                    .Last();

                A.CallTo(() => scopeStatusManager.MarkAsFailed())
                            .MustHaveHappened(Repeated.Exactly.Once);

                ILogger logger = TestDependencyManager.CurrentTestDependencyManager
                    .Objects.OfType<ILogger>()
                    .Last();

                A.CallTo(() => logger.LogFatalAsync(A<string>.Ignored))
                            .MustHaveHappened(Repeated.Exactly.Once);

                IEnumerable<LogData> logData = logger.LogData;

                logData.Single(c => c.Key == "ResponseStatusCode" && (string)c.Value == "501");
                logData.Single(c => c.Key == nameof(IRequestInformationProvider.HttpMethod) && (string)c.Value == "GET");
                logData.Single(c => c.Key == "UserId" && (string)c.Value == "ValidUserName");
            }
        }

        [TestMethod]
        [TestCategory("ExceptionHandler"), TestCategory("Logging")]
        public async Task ExceptionHandlerMustSaveWarningToLogStoreBecauseOfErrorRelatedToClientRequest()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment())
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                await testEnvironment.Server.GetHttpClient(token)
                    .GetAsync("/odata/Test/XYZ");

                IScopeStatusManager scopeStatusManager = TestDependencyManager.CurrentTestDependencyManager
                    .Objects.OfType<IScopeStatusManager>()
                    .Last();

                A.CallTo(() => scopeStatusManager.MarkAsFailed())
                    .MustHaveHappened(Repeated.Exactly.Once);

                ILogger logger = TestDependencyManager.CurrentTestDependencyManager
                    .Objects.OfType<ILogger>()
                    .Last();

                A.CallTo(() => logger.LogWarningAsync(A<string>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);

                IEnumerable<LogData> logData = logger.LogData;

                logData.Single(c => c.Key == "ResponseStatusCode" && (string)c.Value == "406");
                logData.Single(c => c.Key == nameof(IRequestInformationProvider.HttpMethod) && (string)c.Value == "GET");
                logData.Single(c => c.Key == "UserId" && (string)c.Value == "ValidUserName");
            }
        }
    }
}
