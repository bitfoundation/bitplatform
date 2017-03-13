/*using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using Foundation.Core.Contracts;
using Foundation.Test.Core.Contracts;
using Foundation.Test.Model.DomainModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Simple.OData.Client;
using Foundation.Test.Api.ApiControllers;

namespace Foundation.Test.Api.Middlewares.WebApi.Tests
{
    [TestClass]
    public class StringFormattersTests
    {
        [Ignore]
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
                    string arg = correctString.GetArgument<string>("source");

                    return "ONE" + arg;
                });

            A.CallTo(() => stringCorrector2.CorrectString(A<string>.Ignored))
                .ReturnsLazily(correctString =>
                {
                    string arg = correctString.GetArgument<string>("source");

                    return "TWO" + arg;
                });

            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs
            {
                AdditionalDependencies = manager =>
                {
                    manager.RegisterInstance(valueChecker);
                    manager.RegisterInstance(stringCorrector2, overwriteExciting: false);
                    manager.RegisterInstance(stringCorrector1, overwriteExciting: false);
                }
            }))
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                ODataClient client = testEnvironment.Server.BuildODataClient(token: token);

                await client.Controller<TestModelsController, TestModel>()
                    .Action(nameof(TestModelsController.StringFormattersTests))
                    .Set(new
                    {
                        simpleString = "simpleString",
                        stringsArray = new[] { "stringsArray1", "stringsArray2" },
                        stringsArray2 = new[] { "stringsArray1", "stringsArray2" },
                        simpleEntity = new TestModel { StringProperty = "StringProperty", Id = 1, Version = 1 },
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
                        }
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
                    .MustHaveHappened(Repeated.Exactly.Times(6));

                A.CallTo(() => stringCorrector2.CorrectString(A<string>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Times(6));
            }
        }

        [Ignore]
        [TestMethod]
        [TestCategory("WebApi")]
        public virtual async Task TestStringFormattersInUrl()
        {
            IStringCorrector stringCorrector = A.Fake<IStringCorrector>();

            A.CallTo(() => stringCorrector.CorrectString(A<string>.Ignored))
                .ReturnsLazily(correctString =>
                {
                    string arg = correctString.GetArgument<string>("source");

                    return arg.Replace("VALUE", "Test");
                });

            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs
            {
                AdditionalDependencies = manager =>
                {
                    manager.RegisterInstance(stringCorrector, overwriteExciting: false);
                }
            }))
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                ODataClient client = testEnvironment.Server.BuildODataClient(token: token);

                IEnumerable<TestModel> testModels = await client.Controller<TestModelsController, TestModel>()
                     .Function(nameof(TestModelsController.GetSomeTestModelsForTest))
                     .Filter(tm => tm.StringProperty == "VALUE")
                     .FindEntriesAsync();

                Assert.AreEqual(1, testModels.Count());
            }
        }
    }
}
*/