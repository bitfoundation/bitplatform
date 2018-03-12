using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Test;
using Bit.Tests.Api.ApiControllers;
using Bit.Tests.Core.Contracts;
using Bit.Tests.Model.DomainModels;
using FakeItEasy;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Simple.OData.Client;

namespace Bit.Tests.Api.Middlewares.WebApi.Tests
{
    [TestClass]
    public class StringFormattersTests
    {
        [TestMethod]
        [TestCategory("WebApi")]
        public virtual async Task TestStringFormattersInCustomActions()
        {
            IValueChecker valueChecker = A.Fake<IValueChecker>();

            IStringCorrector stringCorrector1 = A.Fake<IStringCorrector>();

            IStringCorrector stringCorrector2 = A.Fake<IStringCorrector>();

            A.CallTo(() => stringCorrector1.CorrectString(A<string>.Ignored))
                .ReturnsLazily(correctString =>
                {
                    string arg = correctString.GetArgument<string>("input");

                    return "ONE" + arg;
                });

            A.CallTo(() => stringCorrector2.CorrectString(A<string>.Ignored))
                .ReturnsLazily(correctString =>
                {
                    string arg = correctString.GetArgument<string>("input");

                    return "TWO" + arg;
                });

            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs
            {
                AdditionalDependencies = manager =>
                {
                    manager.RegisterInstance(valueChecker);
                    manager.RegisterInstance(stringCorrector2, overwriteExciting: false);
                    manager.RegisterInstance(stringCorrector1, overwriteExciting: false);
                }
            }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                IODataClient client = testEnvironment.Server.BuildODataClient(token: token);

                await client.Controller<TestModelsController, TestModel>()
                    .Action(nameof(TestModelsController.StringFormattersTests))
                    .Set(new TestModelsController.StringFormattersTestsParameters
                    {
                        simpleString = "simpleString",
                        stringsArray = new[] { "stringsArray1", "stringsArray2" },
                        stringsArray2 = new[] { "stringsArray1", "stringsArray2" },
                        entitiesArray = new[]
                        {
                            new TestModel
                            {
                                StringProperty = "StringProperty1", Id = 2, Version = 2
                            },
                            new TestModel
                            {
                                StringProperty = "StringProperty2", Id = 3, Version = 3
                            }
                        },
                        simpleDto = new TestModel { StringProperty = "StringProperty", Id = 1, Version = 1 }
                    }).ExecuteAsync();

                A.CallTo(() => valueChecker.CheckValue("ONETWOsimpleString"))
                    .MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => valueChecker.CheckValue(A<List<string>>.That.Matches(strs => strs.SequenceEqual(new List<string> { "ONETWOstringsArray1", "ONETWOstringsArray2" }))))
                    .MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => valueChecker.CheckValue(A<TestModel>.That.Matches(tm => tm.StringProperty == "ONETWOStringProperty")))
                    .MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => valueChecker.CheckValue(A<List<TestModel>>.That.Matches(tms => tms.First().StringProperty == "ONETWOStringProperty1" && tms.Last().StringProperty == "ONETWOStringProperty2")))
                    .MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => stringCorrector1.CorrectString(A<string>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Times(8));

                A.CallTo(() => stringCorrector2.CorrectString(A<string>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Times(8));
            }
        }

        [TestMethod]
        [TestCategory("WebApi")]
        public virtual async Task TestStringFormattersInUrl()
        {
            IStringCorrector stringCorrector = A.Fake<IStringCorrector>();

            A.CallTo(() => stringCorrector.CorrectString(A<string>.Ignored))
                .ReturnsLazily(correctString =>
                {
                    string arg = correctString.GetArgument<string>("input");

                    return arg.Replace("VALUE", "Test");
                });

            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs
            {
                AdditionalDependencies = manager =>
                {
                    manager.RegisterInstance(stringCorrector, overwriteExciting: false);
                }
            }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                IODataClient client = testEnvironment.Server.BuildODataClient(token: token);

                IEnumerable<TestModel> testModels = await client.Controller<TestModelsController, TestModel>()
                     .Function(nameof(TestModelsController.GetSomeTestModelsForTest))
                     .Filter(tm => tm.StringProperty == "VALUE")
                     .FindEntriesAsync();

                Assert.AreEqual(1, testModels.Count());
            }
        }
    }
}