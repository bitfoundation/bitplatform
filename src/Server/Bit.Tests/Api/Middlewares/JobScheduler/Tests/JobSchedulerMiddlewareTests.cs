using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bit.Tests.Core.Contracts;
using FakeItEasy;
using Bit.Test;
using Simple.OData.Client;
using Bit.Tests.Api.ApiControllers;
using Bit.Tests.Model.DomainModels;
using System;
using Bit.Api.ApiControllers;
using Bit.Core.Contracts;
using Bit.Model.Dtos;
using Bit.Test.Core.Implementations;
using System.Linq;

namespace Bit.Tests.Api.Middlewares.JobScheduler.Tests
{
    [TestClass]
    public class JobSchedulerMiddlewareTests
    {
        [TestMethod]
        [TestCategory("BackgroundJobs"), TestCategory("Security")]
        public async Task LoggedInUserMustHaveAccessToJobsDashboard()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

                HttpResponseMessage getDefaultPageResponse = await testEnvironment.Server.GetHttpClient(token).GetAsync("/jobs");

                Assert.AreEqual(HttpStatusCode.OK, getDefaultPageResponse.StatusCode);

                Assert.AreEqual("text/html", getDefaultPageResponse.Content.Headers.ContentType.MediaType);
            }
        }

        [TestMethod]
        [TestCategory("BackgroundJobs"), TestCategory("Security")]
        public async Task NotLoggedInUserMustNotHaveAccessToJobsDashboard()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                HttpResponseMessage getDefaultPageResponse = await testEnvironment.Server.GetHttpClient()
                    .GetAsync("/jobs");

                Assert.AreEqual(HttpStatusCode.Unauthorized, getDefaultPageResponse.StatusCode);
            }
        }

        [TestMethod]
        [TestCategory("BackgroundJobs"), TestCategory("WebApi")]
        public async Task SendEmailUsingBackgroundJobWorkerAndWebApi()
        {
            IEmailService emailService = A.Fake<IEmailService>();

            TaskCompletionSource<bool> emailSent = new TaskCompletionSource<bool>();

            A.CallTo(() => emailService.SendEmail(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Invokes(() =>
                {
                    emailSent.SetResult(true);
                });

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

                string jobId = (await client.Controller<TestModelsController, TestModel>()
                    .Action(nameof(TestModelsController.SendEmailUsingBackgroundJobService))
                    .Set(new TestModelsController.EmailParameters { to = "Someone", title = "Email title", message = "Email message" })
                    .ExecuteAsScalarAsync<Guid>()).ToString();

                ODataClient bitODataClient = testEnvironment.Server.BuildODataClient(token: token, route: "Bit");

                JobInfoDto jobInfo = await bitODataClient.Controller<JobsInfoController, JobInfoDto>()
                    .Key(jobId)
                    .FindEntryAsync();

                Assert.AreEqual(true, await emailSent.Task);

                await Task.Delay(TimeSpan.FromSeconds(1));

                jobInfo = await bitODataClient.Controller<JobsInfoController, JobInfoDto>()
                    .Key(jobId)
                    .FindEntryAsync();

                Assert.AreEqual("Succeeded", jobInfo.State);
            }
        }

        [TestMethod]
        [TestCategory("BackgroundJobs"), TestCategory("WebApi")]
        public async Task SendEmailUsingBackgroundJobWorkerAndWebApiAndThenPushToReceiver()
        {
            IEmailService emailService = A.Fake<IEmailService>();

            TaskCompletionSource<bool> emailSent = new TaskCompletionSource<bool>();

            A.CallTo(() => emailService.SendEmail(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Invokes(() =>
                {
                    emailSent.SetResult(true);
                });

            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs
            {
                AdditionalDependencies = manager =>
                {
                    manager.RegisterInstance(emailService);
                }
            }))
            {
                TokenResponse someoneToken = await testEnvironment.Server.Login("SomeOne", "ValidPassword", clientName: "TestResOwner");

                TaskCompletionSource<bool> onMessageReceivedCalled = new TaskCompletionSource<bool>();

                await testEnvironment.Server.BuildSignalRClient(someoneToken, (messageKey, messageArgs) =>
                {
                    onMessageReceivedCalled.SetResult(true);
                });

                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

                ODataClient client = testEnvironment.Server.BuildODataClient(token: token);

                string jobId = (await client.Controller<TestModelsController, TestModel>()
                    .Action(nameof(TestModelsController.SendEmailUsingBackgroundJobServiceAndPushAfterThat))
                    .Set(new TestModelsController.EmailParameters { to = "SomeOne", title = "Email title", message = "Email message" })
                    .ExecuteAsScalarAsync<Guid>()).ToString();

                ODataClient bitODataClient = testEnvironment.Server.BuildODataClient(token: token, route: "Bit");

                JobInfoDto jobInfo = await bitODataClient.Controller<JobsInfoController, JobInfoDto>()
                    .Key(jobId)
                    .FindEntryAsync();

                Assert.AreEqual(true, await emailSent.Task);
                Assert.AreEqual(true, await onMessageReceivedCalled.Task);

                await Task.Delay(TimeSpan.FromSeconds(1));

                jobInfo = await bitODataClient.Controller<JobsInfoController, JobInfoDto>()
                    .Key(jobId)
                    .FindEntryAsync();

                Assert.AreEqual("Succeeded", jobInfo.State);
            }
        }

        [TestMethod]
        [TestCategory("BackgroundJobs"), TestCategory("Logging")]
        [Ignore]
        public async Task LogExceptionWhenEmailSendFailedAndTryForTheSecondTime()
        {
            IEmailService emailService = A.Fake<IEmailService>();

            TaskCompletionSource<bool> emailSent = new TaskCompletionSource<bool>();

            int tryCount = 0;

            A.CallTo(() => emailService.SendEmail(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Invokes(() =>
                {
                    tryCount++;

                    if (tryCount == 2)
                    {
                        emailSent.SetResult(true);
                        return;
                    }

                    throw new InvalidOperationException();
                });

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

                string jobId = (await client.Controller<TestModelsController, TestModel>()
                    .Action(nameof(TestModelsController.SendEmailUsingBackgroundJobService))
                    .Set(new TestModelsController.EmailParameters { to = "Someone", title = "Email title", message = "Email message" })
                    .ExecuteAsScalarAsync<Guid>()).ToString();

                Assert.AreEqual(true, await emailSent.Task);

                ILogger logger = TestDependencyManager.CurrentTestDependencyManager
                   .Objects.OfType<ILogger>()
                   .Last();

                A.CallTo(() => logger.LogException(A<Exception>.That.Matches(e => e is InvalidOperationException), A<string>.That.Matches(errMsg => errMsg.Contains(jobId))))
                    .MustHaveHappened(Repeated.Exactly.Once);

                Assert.AreEqual(2, tryCount);
            }
        }
    }
}