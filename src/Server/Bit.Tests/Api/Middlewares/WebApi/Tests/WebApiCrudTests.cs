using Bit.Data.Contracts;
using Bit.Http.Contracts;
using Bit.Tests.Api.ApiControllers;
using Bit.Tests.Core.Contracts;
using Bit.Tests.Model.DomainModels;
using FakeItEasy;
using Bit.Http.Contracts;
using Microsoft.AspNet.OData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Simple.OData.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

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
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                IODataClient client = testEnvironment.BuildTestODataClient(token: token);

                TestModel modelBeforeInsert = new TestModel
                {
                    StringProperty = "Test",
                    Version = 1
                };

                TestModel modelAfterInsert = await client.TestModels()
                    .Set(modelBeforeInsert)
                    .CreateEntryAsync();

                Assert.AreNotEqual(0, modelAfterInsert.Id);

                TestModelsController testModelsController = testEnvironment.GetObjects<TestModelsController>()
                    .Last();

                A.CallTo(() => testModelsController.Create(A<TestModel>.That.Matches(testModel => testModel.StringProperty == "Test"),
                            A<CancellationToken>.Ignored))
                    .MustHaveHappenedOnceExactly();

                IRepository<TestModel> testModelsRepository =
                    testEnvironment.GetObjects<IRepository<TestModel>>()
                        .Single();

                A.CallTo(() => testModelsRepository.AddAsync(A<TestModel>.That.Matches(testModel => testModel.StringProperty == "Test"), A<CancellationToken>.Ignored))
                    .MustHaveHappenedOnceExactly();
            }

            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                HttpClient client = testEnvironment.Server.BuildHttpClient(token: token);

                TestModel modelBeforeInsert = new TestModel
                {
                    StringProperty = "Test",
                    Version = 1
                };

                TestModel modelAfterInsert = await client.TestModels()
                    .Create(modelBeforeInsert);

                Assert.AreNotEqual(0, modelAfterInsert.Id);

                TestModelsController testModelsController = testEnvironment.GetObjects<TestModelsController>()
                    .Last();

                A.CallTo(() => testModelsController.Create(A<TestModel>.That.Matches(testModel => testModel.StringProperty == "Test"),
                            A<CancellationToken>.Ignored))
                    .MustHaveHappenedOnceExactly();

                IRepository<TestModel> testModelsRepository =
                    testEnvironment.GetObjects<IRepository<TestModel>>()
                        .Single();

                A.CallTo(() => testModelsRepository.AddAsync(A<TestModel>.That.Matches(testModel => testModel.StringProperty == "Test"), A<CancellationToken>.Ignored))
                    .MustHaveHappenedOnceExactly();
            }
        }

        [TestMethod]
        [TestCategory("WebApi"), TestCategory("OData")]
        public async Task TestPartialUpdate()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                IODataClient client = testEnvironment.BuildTestODataClient(token: token);

                long modelBeforeUpdateId = await client.TestModels()
                    .Top(1)
                    .Select(t => t.Id)
                    .FindScalarAsync<long>();

                TestModel modelAfterUpdate = await client.TestModels()
                    .Key(modelBeforeUpdateId)
                    .Set(new { StringProperty = "Test2" })
                    .UpdateEntryAsync();

                Assert.AreEqual("Test2", modelAfterUpdate.StringProperty);

                TestModelsController testModelsController = testEnvironment.GetObjects<TestModelsController>()
                    .Last();

                A.CallTo(() => testModelsController.PartialUpdate(modelBeforeUpdateId,
                    A<Delta<TestModel>>.That.Matches(
                        testModelDelta =>
                            testModelDelta.GetChangedPropertyNames().Single() == nameof(TestModel.StringProperty)),
                    A<CancellationToken>.Ignored))
                    .MustHaveHappenedOnceExactly();

                IRepository<TestModel> testModelsRepository =
                    testEnvironment.GetObjects<IRepository<TestModel>>()
                        .Last();

                A.CallTo(() => testModelsRepository.UpdateAsync(
                            A<TestModel>.That.Matches(testModel => testModel.StringProperty == "Test2"),
                            A<CancellationToken>.Ignored))
                    .MustHaveHappenedOnceExactly();
            }

            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                HttpClient client = testEnvironment.Server.BuildHttpClient(token: token);

                long modelBeforeUpdateId = (await client.TestModels()
                    .Get(oDataContext: new ODataContext { Query = "$top=1&$select=Id" }))
                    .First().Id;

                TestModel modelAfterUpdate = await client.TestModels()
                    .PartialUpdate(new object[] { modelBeforeUpdateId }, new { StringProperty = "Test2" });

                Assert.AreEqual("Test2", modelAfterUpdate.StringProperty);

                TestModelsController testModelsController = testEnvironment.GetObjects<TestModelsController>()
                    .Last();

                A.CallTo(() => testModelsController.PartialUpdate(modelBeforeUpdateId,
                    A<Delta<TestModel>>.That.Matches(
                        testModelDelta =>
                            testModelDelta.GetChangedPropertyNames().Single() == nameof(TestModel.StringProperty)),
                    A<CancellationToken>.Ignored))
                    .MustHaveHappenedOnceExactly();

                IRepository<TestModel> testModelsRepository =
                    testEnvironment.GetObjects<IRepository<TestModel>>()
                        .Last();

                A.CallTo(() => testModelsRepository.UpdateAsync(
                            A<TestModel>.That.Matches(testModel => testModel.StringProperty == "Test2"),
                            A<CancellationToken>.Ignored))
                    .MustHaveHappenedOnceExactly();
            }
        }

        [TestMethod]
        [TestCategory("WebApi"), TestCategory("OData")]
        public async Task TestDelete()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                IODataClient client = testEnvironment.BuildTestODataClient(token: token);

                long modelIdForDelete = await client.TestModels()
                    .Top(1)
                    .Select(t => t.Id)
                    .FindScalarAsync<long>();

                await client.TestModels()
                    .Key(modelIdForDelete)
                    .DeleteEntryAsync();

                TestModelsController testModelsController = testEnvironment.GetObjects<TestModelsController>()
                    .Last();

                A.CallTo(() => testModelsController.Delete(modelIdForDelete, A<CancellationToken>.Ignored))
                    .MustHaveHappenedOnceExactly();

                IRepository<TestModel> testModelsRepository =
                    testEnvironment.GetObjects<IRepository<TestModel>>()
                        .Last();

                A.CallTo(() => testModelsRepository.DeleteAsync(
                            A<TestModel>.That.Matches(testModel => testModel.Id == modelIdForDelete),
                            A<CancellationToken>.Ignored))
                    .MustHaveHappenedOnceExactly();
            }

            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                HttpClient client = testEnvironment.Server.BuildHttpClient(token: token);

                long modelIdForDelete = (await client.TestModels()
                    .Get(oDataContext: new ODataContext { Query = "$top=1&$select=Id" }))
                    .First().Id;

                await client.TestModels()
                    .Delete(new object[] { modelIdForDelete });

                TestModelsController testModelsController = testEnvironment.GetObjects<TestModelsController>()
                    .Last();

                A.CallTo(() => testModelsController.Delete(modelIdForDelete, A<CancellationToken>.Ignored))
                    .MustHaveHappenedOnceExactly();

                IRepository<TestModel> testModelsRepository =
                    testEnvironment.GetObjects<IRepository<TestModel>>()
                        .Last();

                A.CallTo(() => testModelsRepository.DeleteAsync(
                            A<TestModel>.That.Matches(testModel => testModel.Id == modelIdForDelete),
                            A<CancellationToken>.Ignored))
                    .MustHaveHappenedOnceExactly();
            }
        }

        [TestMethod]
        [TestCategory("WebApi"), TestCategory("OData")]
        public async Task TestSimpleFilter()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                IRequestValidator requestValidator = A.Fake<IRequestValidator>();

                IODataClient client = testEnvironment.BuildTestODataClient(token: token, odataClientSettings: new ODataClientSettings
                {
                    BeforeRequest = message =>
                    {
                        requestValidator.ValidateRequestByUri(message.RequestUri);
                    }
                });

                IEnumerable<ParentEntity> parentEntities = await client.ParentEntities()
                    .Where(p => p.Name == "A")
                    .FindEntriesAsync();

                Assert.AreEqual(1, parentEntities.Count());

                A.CallTo(() => requestValidator.ValidateRequestByUri(A<Uri>.That.Matches(uri => uri.ToString().AsUnescaped().Contains("$filter=Name eq 'A'"))))
                    .MustHaveHappenedOnceExactly();
            }
        }

        [TestMethod]
        [TestCategory("WebApi"), TestCategory("OData")]
        public async Task TestComplexProjection()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                HttpResponseMessage response = await testEnvironment.Server.BuildHttpClient(token)
                            .GetAsync("/odata/Test/ChildEntities?$top=1&$select=Id,Name&$expand=ParentEntity($select=Id,Name)");

                response.EnsureSuccessStatusCode();
            }
        }
    }
}
