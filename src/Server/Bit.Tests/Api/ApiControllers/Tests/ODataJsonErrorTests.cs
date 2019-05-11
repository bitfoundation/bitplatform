using Bit.Core.Contracts;
using Bit.Test.Implementations;
using FakeItEasy;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Bit.Tests.Api.ApiControllers.Tests
{
    [TestClass]
    public class ODataJsonErrorTests
    {
        [TestMethod]
        [TestCategory("WebApi"), TestCategory("Logging")]
        public virtual async Task JsonParseErrorTest()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                HttpClient httpClient = testEnvironment.Server.BuildHttpClient(token);

                HttpResponseMessage response = await httpClient.PostAsJsonAsync("odata/Test/DtoWithEnum", new
                {
                    Id = "SOME_STRING_INSTEAD_OF_INT",
                    Gender = "Man",
                    Test = "string"
                });

                const string expectedErrorMessage = "Newtonsoft.Json.JsonReaderException: Could not convert string to integer: SOME_STRING_INSTEAD_OF_INT. Path 'Id', line 1, position 34.";

                Assert.AreEqual(false, response.IsSuccessStatusCode);
                using (StreamReader reader = new StreamReader(await response.Content.ReadAsStreamAsync()))
                using (JsonTextReader jsonReader = new JsonTextReader(reader))
                {
                    string errMessage = (await JToken.LoadAsync(jsonReader))["error"].Value<string>("message");
                    Assert.IsTrue(errMessage.Contains(expectedErrorMessage));
                }

                ILogger logger = TestDependencyManager.CurrentTestDependencyManager
                    .Objects.OfType<ILogger>()
                    .Last();

                A.CallTo(() => logger.AddLogData("WebException", A<string>.That.Matches(exp => exp.Contains(expectedErrorMessage))))
                    .MustHaveHappenedOnceExactly();
            }
        }
    }
}
