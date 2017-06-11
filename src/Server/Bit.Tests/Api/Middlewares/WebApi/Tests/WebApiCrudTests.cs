using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bit.Tests.Api.ApiControllers;
using Simple.OData.Client;
using Bit.Tests.Model.DomainModels;
using Bit.Test.Core.Implementations;
using FakeItEasy;
using Bit.Tests.Core.Contracts;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using Bit.Data.Contracts;
using System.Web.OData;
using System;

namespace Bit.Tests.Api.Middlewares.WebApi.Tests
{
    [TestClass]
    public class WebApiCrudTests
    {
        [TestMethod]
        [TestCategory("WebApi"), TestCategory("OData")]
        public async Task TestInsert()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

                ODataClient client = testEnvironment.Server.BuildODataClient(token: token);

                TestModel modelBeforeInsert = new TestModel
                {
                    StringProperty = "Test",
                    Version = 1
                };

                TestModel modelAfterInsert = await client.Controller<TestModelsController, TestModel>()
                    .Set(modelBeforeInsert)
                    .InsertEntryAsync();

                Assert.AreNotEqual(0, modelAfterInsert.Id);

                TestModelsController testModelsController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>()
                    .Last();

                A.CallTo(() => testModelsController.Create(A<TestModel>.That.Matches(testModel => testModel.StringProperty == "Test"),
                            A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);

                IRepository<TestModel> testModelsRepository =
                    TestDependencyManager.CurrentTestDependencyManager.Objects
                        .OfType<IRepository<TestModel>>()
                        .Single();

                A.CallTo(() => testModelsRepository.AddAsync(A<TestModel>.That.Matches(testModel => testModel.StringProperty == "Test"), A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }

        [TestMethod]
        [TestCategory("WebApi"), TestCategory("OData")]
        public async Task TestPartialUpdate()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

                ODataClient client = testEnvironment.Server.BuildODataClient(token: token);

                long modelBeforeUpdateId = await client.Controller<TestModelsController, TestModel>()
                    .Top(1)
                    .Select(t => t.Id)
                    .FindScalarAsync<long>();

                TestModel modelAfterUpdate = await client.Controller<TestModelsController, TestModel>()
                    .Key(modelBeforeUpdateId)
                    .Set(new { StringProperty = "Test2" })
                    .UpdateEntryAsync();

                Assert.AreEqual("Test2", modelAfterUpdate.StringProperty);

                TestModelsController testModelsController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>()
                    .Last();

                A.CallTo(() => testModelsController.PartialUpdate(modelBeforeUpdateId,
                    A<Delta<TestModel>>.That.Matches(
                        testModelDelta =>
                            testModelDelta.GetChangedPropertyNames().Single() == nameof(TestModel.StringProperty)),
                    A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);

                IRepository<TestModel> testModelsRepository =
                    TestDependencyManager.CurrentTestDependencyManager.Objects
                        .OfType<IRepository<TestModel>>()
                        .Last();

                A.CallTo(() => testModelsRepository.UpdateAsync(
                            A<TestModel>.That.Matches(testModel => testModel.StringProperty == "Test2"),
                            A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }

        [TestMethod]
        [TestCategory("WebApi"), TestCategory("OData")]
        public async Task TestDelete()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

                ODataClient client = testEnvironment.Server.BuildODataClient(token: token);

                long modelIdForDelete = await client.Controller<TestModelsController, TestModel>()
                    .Top(1)
                    .Select(t => t.Id)
                    .FindScalarAsync<long>();

                await client.Controller<TestModelsController, TestModel>()
                    .Key(modelIdForDelete)
                    .DeleteEntryAsync();

                TestModelsController testModelsController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>()
                    .Last();

                A.CallTo(() => testModelsController.Delete(modelIdForDelete, A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);

                IRepository<TestModel> testModelsRepository =
                    TestDependencyManager.CurrentTestDependencyManager.Objects
                        .OfType<IRepository<TestModel>>()
                        .Last();

                A.CallTo(() => testModelsRepository.DeleteAsync(
                            A<TestModel>.That.Matches(testModel => testModel.Id == modelIdForDelete),
                            A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }

        [TestMethod]
        [TestCategory("WebApi"), TestCategory("OData")]
        public async Task TestSimpleFilter()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

                IRequestValidator requestValidator = A.Fake<IRequestValidator>();

                ODataClient client = testEnvironment.Server.BuildODataClient(token: token, beforeRequest: message =>
                {
                    requestValidator.ValidateRequestByUri(message.RequestUri);
                });

                IEnumerable<ParentEntity> parentEntities = await client.Controller<ParentEntitiesController, ParentEntity>()
                    .Filter(p => p.Name == "A")
                    .FindEntriesAsync();

                Assert.AreEqual(1, parentEntities.Count());

                A.CallTo(() => requestValidator.ValidateRequestByUri(A<Uri>.That.Matches(uri => uri.ToString().Contains("$filter=Name eq 'A'"))))
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }

        [TestMethod]
        [TestCategory("WebApi"), TestCategory("OData")]
        public async Task TestComplexProjection()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

                HttpResponseMessage response = await testEnvironment.Server.GetHttpClient(token)
                            .GetAsync("/odata/Test/ChildEntities?$top=1&$select=Id,Name&$expand=ParentEntity($select=Id,Name)");

                response.EnsureSuccessStatusCode();
            }
        }
    }
}