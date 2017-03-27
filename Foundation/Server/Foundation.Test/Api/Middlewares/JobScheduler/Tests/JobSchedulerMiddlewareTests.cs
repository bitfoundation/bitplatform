using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Foundation.Test.Api.Middlewares.JobScheduler.Tests
{
    [TestClass]
    public class JobSchedulerMiddlewareTests
    {
        [TestMethod]
        [TestCategory("BackgroundJobs"), TestCategory("Security")]
        public async Task LoggedInUserMustHaveAccessToJobsDashboard()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment())
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                HttpResponseMessage getDefaultPageResponse = await testEnvironment.Server.GetHttpClient(token).GetAsync("/jobs");

                Assert.AreEqual(HttpStatusCode.OK, getDefaultPageResponse.StatusCode);

                Assert.AreEqual("text/html", getDefaultPageResponse.Content.Headers.ContentType.MediaType);
            }
        }

        [TestMethod]
        [TestCategory("BackgroundJobs"), TestCategory("Security")]
        public async Task NotLoggedInUserMustNotHaveAccessToJobsDashboard()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment())
            {
                HttpResponseMessage getDefaultPageResponse = await testEnvironment.Server.GetHttpClient()
                    .GetAsync("/jobs");

                Assert.AreEqual(HttpStatusCode.Unauthorized, getDefaultPageResponse.StatusCode);
            }
        }

        /*[Ignore]
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

                string jobId = (await client.Controller<TestModelsController, TestModel>()
                    .Action(nameof(TestModelsController.SendEmailUsingBackgroundJobService))
                    .Set(new { to = "Someone", title = "Email title", message = "Email message" })
                    .ExecuteAsScalarAsync<Guid>()).ToString();

                ODataClient foundationClient = testEnvironment.Server.BuildODataClient(token: token, route: "Foundation");

                JobInfo jobInfo = await foundationClient.Controller<JobsInfoController, JobInfo>()
                    .Key(jobId)
                    .FindEntryAsync();

                Assert.AreEqual("Enqueued", jobInfo.State);

                Assert.AreEqual(true, await emailSent.Task);

                await Task.Delay(TimeSpan.FromSeconds(1));

                jobInfo = await foundationClient.Controller<JobsInfoController, JobInfo>()
                    .Key(jobId)
                    .FindEntryAsync();

                Assert.AreEqual("Succeeded", jobInfo.State);
            }
        }

        [Ignore]
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

            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs
            {
                AdditionalDependencies = manager =>
                {
                    manager.RegisterInstance(emailService);
                }
            }))
            {
                OAuthToken someoneToken = testEnvironment.Server.Login("SomeOne", "ValidPassword");

                TaskCompletionSource<bool> onMessageReceivedCalled = new TaskCompletionSource<bool>();

                testEnvironment.Server.BuildSignalRClient(someoneToken, (messageKey, messageArgs) =>
                {
                    onMessageReceivedCalled.SetResult(true);
                });

                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                ODataClient client = testEnvironment.Server.BuildODataClient(token: token);

                string jobId = (await client.Controller<TestModelsController, TestModel>()
                    .Action(nameof(TestModelsController.SendEmailUsingBackgroundJobServiceAndPushAfterThat))
                    .Set(new { to = "SomeOne", title = "Email title", message = "Email message" })
                    .ExecuteAsScalarAsync<Guid>()).ToString();

                ODataClient foundationClient = testEnvironment.Server.BuildODataClient(token: token, route: "Foundation");

                JobInfo jobInfo = await foundationClient.Controller<JobsInfoController, JobInfo>()
                    .Key(jobId)
                    .FindEntryAsync();

                Assert.AreEqual("Awaiting", jobInfo.State);

                Assert.AreEqual(true, await emailSent.Task);

                await Task.Delay(TimeSpan.FromSeconds(1));

                jobInfo = await foundationClient.Controller<JobsInfoController, JobInfo>()
                    .Key(jobId)
                    .FindEntryAsync();

                Assert.AreEqual("Succeeded", jobInfo.State);
            }
        }

        [Ignore]
        [TestMethod]
        [TestCategory("BackgroundJobs"), TestCategory("Logging")]
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

                string jobId = (await client.Controller<TestModelsController, TestModel>()
                    .Action(nameof(TestModelsController.SendEmailUsingBackgroundJobService))
                    .Set(new { to = "Someone", title = "Email title", message = "Email message" })
                    .ExecuteAsScalarAsync<Guid>()).ToString();

                Assert.AreEqual(true, await emailSent.Task);

                //A.CallTo(() => DefaultLogger.Current.LogException(A<Exception>.That.Matches(e => e is InvalidOperationException), A<string>.That.Matches(errMsg => errMsg.Contains(jobId))))
                //    .MustHaveHappened(Repeated.Exactly.Once);

                Assert.AreEqual(2, tryCount);
            }
        }*/
    }
}