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
using Bit.OData.ODataControllers;
using Bit.Core.Contracts;
using Bit.Model.Dtos;
using Bit.Test.Implementations;
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
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                HttpResponseMessage getIndexPageResponse = await testEnvironment.Server.BuildHttpClient(token).GetAsync("/jobs");

                Assert.AreEqual(HttpStatusCode.OK, getIndexPageResponse.StatusCode);

                Assert.AreEqual("text/html", getIndexPageResponse.Content.Headers.ContentType.MediaType);
            }
        }

        [TestMethod]
        [TestCategory("BackgroundJobs"), TestCategory("Security")]
        public async Task NotLoggedInUserMustNotHaveAccessToJobsDashboard()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                HttpResponseMessage getIndexPageResponse = await testEnvironment.Server.BuildHttpClient()
                    .GetAsync("/jobs");

                Assert.AreEqual(HttpStatusCode.Unauthorized, getIndexPageResponse.StatusCode);
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
                AdditionalDependencies = (manager, services) =>
                {
                    manager.RegisterInstance(emailService);
                }
            }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                IODataClient client = testEnvironment.Server.BuildODataClient(token: token);

                string jobId = (await client.TestModels()
                    .SendEmailUsingBackgroundJobService("Someone", "Email title", "Email message")
                    .ExecuteAsScalarAsync<Guid>()).ToString();

                IODataClient bitODataClient = testEnvironment.Server.BuildODataClient(token: token, odataRouteName: "Bit");

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
                AdditionalDependencies = (manager, services) =>
                {
                    manager.RegisterInstance(emailService);
                }
            }))
            {
                TokenResponse someoneToken = await testEnvironment.Server.Login("SomeOne", "ValidPassword", clientId: "TestResOwner");

                TaskCompletionSource<bool> onMessageReceivedCalled = new TaskCompletionSource<bool>();

                await testEnvironment.Server.BuildSignalRClient(someoneToken, (messageKey, messageArgs) =>
                {
                    onMessageReceivedCalled.SetResult(true);
                });

                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                IODataClient client = testEnvironment.Server.BuildODataClient(token: token);

                string jobId = (await client.TestModels()
                                    .SendEmailUsingBackgroundJobServiceAndPushAfterThat(to: "SomeOne", title: "Email title", message: "Email message")
                                    .ExecuteAsScalarAsync<Guid>()).ToString();

                IODataClient bitODataClient = testEnvironment.Server.BuildODataClient(token: token, odataRouteName: "Bit");

                JobInfoDto jobInfo = await bitODataClient.JobsInfo()
                    .Key(jobId)
                    .FindEntryAsync();

                Assert.AreEqual(true, await emailSent.Task);
                Assert.AreEqual(true, await onMessageReceivedCalled.Task);

                await Task.Delay(TimeSpan.FromSeconds(1));

                jobInfo = await bitODataClient.JobsInfo()
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
                AdditionalDependencies = (manager, services) =>
                {
                    manager.RegisterInstance(emailService);
                }
            }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                IODataClient client = testEnvironment.Server.BuildODataClient(token: token);

                string jobId = (await client.TestModels()
                    .SendEmailUsingBackgroundJobService(to: "Someone", title: "Email title", message: "Email message")
                    .ExecuteAsScalarAsync<Guid>()).ToString();

                Assert.AreEqual(true, await emailSent.Task);

                ILogger logger = TestDependencyManager.CurrentTestDependencyManager
                   .Objects.OfType<ILogger>()
                   .Last();

                A.CallTo(() => logger.LogException(A<Exception>.That.Matches(e => e is InvalidOperationException), A<string>.That.Matches(errMsg => errMsg.Contains(jobId))))
                    .MustHaveHappenedOnceExactly();

                Assert.AreEqual(2, tryCount);
            }
        }
    }
}