using Bit.Core.Contracts;
using Bit.Data.Contracts;
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
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Tests.Api.Middlewares.WebApi.Tests
{
    [TestClass]
    public class BatchRequestsTests
    {
        [TestMethod]
        [TestCategory("WebApi")]
        public virtual async Task InsertAndUpdateTogether()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                ODataBatch batchClient = testEnvironment.Server.BuildODataBatchClient(token: token);

                IODataClient client = testEnvironment.Server.BuildODataClient(token: token);

                TestModel modelBeforeInsert = new TestModel
                {
                    StringProperty = "Test",
                    Version = 1
                };

                batchClient += bc => bc.TestModels()
                    .Set(modelBeforeInsert)
                    .CreateEntryAsync();

                long modelBeforeUpdateId = await client.TestModels()
                        .Top(1)
                        .Select(t => t.Id)
                        .FindScalarAsync<long>();

                batchClient += bc => bc.TestModels()
                     .Key(modelBeforeUpdateId)
                     .Set(new { StringProperty = "Test2" })
                     .UpdateEntryAsync();

                await batchClient.ExecuteAsync();

                IRepository<TestModel> testModelsRepository =
                    TestDependencyManager.CurrentTestDependencyManager.Objects
                        .OfType<IRepository<TestModel>>()
                        .Last();

                A.CallTo(() => testModelsRepository.UpdateAsync(
                            A<TestModel>.That.Matches(testModel => testModel.StringProperty == "Test2"),
                            A<CancellationToken>.Ignored))
                    .MustHaveHappenedOnceExactly();

                A.CallTo(() => testModelsRepository.AddAsync(A<TestModel>.That.Matches(testModel => testModel.StringProperty == "Test"), A<CancellationToken>.Ignored))
                    .MustHaveHappenedOnceExactly();
            }
        }

        [TestMethod]
        [TestCategory("WebApi")]
        public virtual async Task ServerMustStopRequestExecutionOnFirstException()
        {
            // This is based on client's request.

            IEmailService emailService = A.Fake<IEmailService>();

            A.CallTo(() => emailService.SendEmail(A<string>.That.Matches(s => s == "Exception"), A<string>.Ignored, A<string>.Ignored))
                .Throws<InvalidOperationException>();

            A.CallTo(() => emailService.SendEmail(A<string>.That.Matches(s => s == "Work"), A<string>.Ignored, A<string>.Ignored))
                .DoesNothing();

            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs
            {
                AdditionalDependencies = (manager, services) =>
                {
                    manager.RegisterInstance(emailService);
                }
            }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                ODataBatch client = testEnvironment.Server.BuildODataBatchClient(token: token, afterResponse: message =>
                {
                    Assert.AreEqual(HttpStatusCode.OK, message.StatusCode);
                    // status code of batch request will be ok all the time, it will be something other than ok,
                    // when the request itself is invalid (BadRequestException).
                });

                try
                {
                    client += c => c.TestModels()
                        .SendEmail(to : "Exception", title : "Email title", message : "Email message")
                        .ExecuteAsync();

                    client += c => c.TestModels()
                                        .SendEmail(to : "Work", title : "Email title", message : "Email message")
                                        .ExecuteAsync();

                    await client.ExecuteAsync();

                    Assert.Fail();
                }
                catch (WebRequestException)
                {
                    A.CallTo(() => emailService.SendEmail(A<string>.That.Matches(s => s == "Exception"), A<string>.Ignored, A<string>.Ignored))
                        .MustHaveHappened();

                    A.CallTo(() => emailService.SendEmail(A<string>.That.Matches(s => s == "Work"), A<string>.Ignored, A<string>.Ignored))
                                            .MustNotHaveHappened();

                    TestModelsController controller = TestDependencyManager.CurrentTestDependencyManager.Objects
                        .OfType<TestModelsController>().Single();

                    ILogger logger = TestDependencyManager.CurrentTestDependencyManager.Objects
                        .OfType<ILogger>().Last();

                    A.CallTo(() => controller.SendEmail(A<TestModelsController.EmailParameters>.Ignored))
                                            .MustHaveHappenedOnceExactly();

                    A.CallTo(() => logger.LogFatalAsync(A<string>.That.Matches(msg => msg == "Scope was failed: Operation is not valid due to the current state of the object.")))
                                            .MustHaveHappenedOnceExactly();
                }
            }
        }
    }
}
