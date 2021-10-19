using Bit.Test;
using Bit.Test.Server;
using Bit.Tests.Api.ApiControllers;
using Bit.Tests.Model.DomainModels;
using Bit.Tests.Model.Dto;
using FakeItEasy;
using Bit.Http.Contracts;
using Microsoft.AspNet.OData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using Simple.OData.Client;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Tests.HtmlClient.ThirdPartyLibrariesTests.JayDataContextTests
{
    [TestClass]
    public class CrudTests
    {
        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataContextOData")]
        public virtual async Task TestInsert()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (WebDriver driver = testEnvironment.Server.BuildWebDriver(new WebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest("testInsert");
                }

                TestModelsController testModelsController = testEnvironment.GetObjects<TestModelsController>()
                    .Single();

                A.CallTo(() => testModelsController.Create(A<TestModel>.That.Matches(m => m.StringProperty == "Test"), A<CancellationToken>.Ignored))
                    .MustHaveHappenedOnceExactly();
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataContextOData")]
        public virtual async Task TestComplexTypeWithOData()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (WebDriver driver = testEnvironment.Server.BuildWebDriver(new WebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest("testComplexTypeWithOData");
                }

                TestComplexController controllerForInsert = testEnvironment.GetObjects<TestComplexController>()
                    .ElementAt(0);

                TestComplexController controllerForUpdate = testEnvironment.GetObjects<TestComplexController>()
                    .ElementAt(2);

                TestComplexController controllerForAction = testEnvironment.GetObjects<TestComplexController>()
                    .ElementAt(3);

                A.CallTo(() => controllerForInsert.Create(A<TestComplexDto>.That.Matches(dto => dto.ComplexObj.Name == "Test?"), A<CancellationToken>.Ignored))
                    .MustHaveHappenedOnceExactly();

                A.CallTo(() => controllerForUpdate.PartialUpdate(1, A<Delta<TestComplexDto>>.That.Matches(dto => dto.GetInstance().ComplexObj.Name == "Test?"), A<CancellationToken>.Ignored))
                    .MustHaveHappenedOnceExactly();

                A.CallTo(() => controllerForAction.DoSomeThingWithComplexObj(A<TestComplexDto>.That.Matches(complexDto => complexDto.ComplexObj.Name == "Test??")))
                    .MustHaveHappenedOnceExactly();
            }
        }

        [TestMethod]
        public virtual async Task TestComplexTypeGetWithCount()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                ODataFeedAnnotations annotations = new ODataFeedAnnotations();

                var result = (await testEnvironment.BuildTestODataClient(token).NestedObjects().GetComplexObjects2()
                    .Take(2)
                    .FindEntriesAsync(annotations)).ToList();

                Assert.AreEqual(5, annotations.Count);
                Assert.AreEqual(2, result.Count);
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataContextOData")]
        public virtual async Task SimpleArrayValuesTest()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (WebDriver driver = testEnvironment.Server.BuildWebDriver(new WebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest("simpleArrayValuesTest");
                }

                TestComplexController controllerForSimpleValuesArray = testEnvironment.GetObjects<TestComplexController>()
                    .Single();

                A.CallTo(() => controllerForSimpleValuesArray.GetValues(A<IEnumerable<int>>.That.Matches(values => values != null), A<CancellationToken>.Ignored))
                    .MustHaveHappenedOnceExactly();
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataContextOData")]
        public virtual async Task EnumTest_HtmlClient()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (WebDriver driver = testEnvironment.Server.BuildWebDriver(new WebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest("enumTest");
                }

                DtoWithEnumController firstCallController = testEnvironment.GetObjects<DtoWithEnumController>()
                    .First();

                DtoWithEnumController secondCallController = testEnvironment.GetObjects<DtoWithEnumController>()
                    .ElementAt(1);

                A.CallTo(() => firstCallController.GetDtoWithEnumsByGender(TestGender.Man))
                    .MustHaveHappenedOnceExactly();

                A.CallTo(() => secondCallController.GetDtoWithEnumsByGender2(TestGender2.Man))
                    .MustHaveHappenedOnceExactly();
            }
        }

        [TestMethod]
        [TestCategory("JayDataContextOData")]
        public virtual async Task EnumTest_CSClient()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                IODataClient client = testEnvironment.BuildTestODataClient(token: token);

                DtoWithEnum dtoWithEnum = await client.DtoWithEnum()
                    .GetDtoWithEnumsByGender(TestGender.Man)
                    .ExecuteAsSingleAsync();

                Assert.AreEqual(TestGender.Man, dtoWithEnum.Gender);

                Assert.AreEqual(true, await client.DtoWithEnum()
                    .PostDtoWithEnum(dtoWithEnum)
                    .ExecuteAsScalarAsync<bool>());

                ODataBatch batchClient = testEnvironment.Server.BuildODataBatchClient(token: token);

                batchClient += bc => bc.DtoWithEnum()
                    .GetDtoWithEnumsByGender2(TestGender2.Man)
                    .ExecuteAsEnumerableAsync();

                batchClient += bc => bc.DtoWithEnum()
                    .GetDtoWithEnumsByGender(TestGender.Man)
                    .ExecuteAsEnumerableAsync();

                await batchClient.ExecuteAsync();

                /*Assert.AreEqual(true, await client.DtoWithEnum()
                    .TestEnumsArray(new[] { TestGender2.Man, TestGender2.Woman })
                    .ExecuteAsScalarAsync<bool>());*/

                DtoWithEnumController firstCallController = testEnvironment.GetObjects<DtoWithEnumController>()
                    .First();

                DtoWithEnumController secondCallController = testEnvironment.GetObjects<DtoWithEnumController>()
                    .ElementAt(2);

                A.CallTo(() => firstCallController.GetDtoWithEnumsByGender(TestGender.Man))
                    .MustHaveHappenedOnceExactly();

                A.CallTo(() => secondCallController.GetDtoWithEnumsByGender2(TestGender2.Man))
                    .MustHaveHappenedOnceExactly();
            }

            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                HttpClient client = testEnvironment.Server.BuildHttpClient(token: token);

                DtoWithEnum dtoWithEnum = (await client.DtoWithEnum()
                    .GetDtoWithEnumsByGender(TestGender.Man)).Single();

                Assert.AreEqual(TestGender.Man, dtoWithEnum.Gender);

                Assert.AreEqual(true, await client.DtoWithEnum()
                    .PostDtoWithEnum(dtoWithEnum));

                await client.DtoWithEnum()
                    .GetDtoWithEnumsByGender2(TestGender2.Man);

                await client.DtoWithEnum()
                    .GetDtoWithEnumsByGender(TestGender.Man);

                /*Assert.AreEqual(true, await client.DtoWithEnum()
                    .TestEnumsArray(new[] { TestGender2.Man, TestGender2.Woman })
                    .ExecuteAsScalarAsync<bool>());*/

                DtoWithEnumController firstCallController = testEnvironment.GetObjects<DtoWithEnumController>()
                    .First();

                DtoWithEnumController secondCallController = testEnvironment.GetObjects<DtoWithEnumController>()
                    .ElementAt(2);

                A.CallTo(() => firstCallController.GetDtoWithEnumsByGender(TestGender.Man))
                    .MustHaveHappenedOnceExactly();

                A.CallTo(() => secondCallController.GetDtoWithEnumsByGender2(TestGender2.Man))
                    .MustHaveHappenedOnceExactly();
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataContextOData")]
        public virtual async Task TestComplexTypeWithOfflineDb()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (WebDriver driver = testEnvironment.Server.BuildWebDriver(new WebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest("testComplexTypeWithOfflineDb");
                }
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataContextOData")]
        public virtual async Task TestGetAllAndFilter()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (WebDriver driver = testEnvironment.Server.BuildWebDriver(new WebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest(@"testGetAllAndFilter");
                }

                ParentEntitiesController parentEntitiesController = testEnvironment.GetObjects<ParentEntitiesController>()
                    .Single();

                A.CallTo(() => parentEntitiesController.Get(A<CancellationToken>.Ignored))
                    .MustHaveHappenedOnceExactly();
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataContextOData")]
        public virtual async Task TestBatchReadRequest()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (WebDriver driver = testEnvironment.Server.BuildWebDriver(new WebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest(@"testBatchReadRequest");
                }

                ParentEntitiesController parentEntitiesController = testEnvironment.GetObjects<ParentEntitiesController>()
                    .Single();

                A.CallTo(() => parentEntitiesController.Get(A<CancellationToken>.Ignored))
                    .MustHaveHappenedOnceExactly();

                TestModelsController testModelsController = testEnvironment.GetObjects<TestModelsController>()
                    .Single();

                A.CallTo(() => testModelsController.Get(A<CancellationToken>.Ignored))
                    .MustHaveHappenedOnceExactly();
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataContextOData")]
        public virtual async Task TestNestedObjects()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (WebDriver driver = testEnvironment.Server.BuildWebDriver(new WebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest("testNested");
                }

                Assert.AreEqual((await testEnvironment.BuildTestODataClient(token)
                    .NestedObjects()
                    .SomeAction(new NestedComplex3 { Obj4 = new NestedComplex4 { Test = NestedEnum.B } }, "Test")
                    .ExecuteAsScalarAsync<NestedEnum>()), NestedEnum.B);
            }
        }
    }
}
