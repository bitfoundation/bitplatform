using Bit.Core.Models;
using Bit.Test;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Bit.Tests.Api.ApiControllers.Tests
{
    [TestClass]
    public class MapperTests
    {
        [TestMethod]
        public virtual async Task TestMapperPerScopeDependencyInjection()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs
            {
                ActiveAppEnvironmentCustomizer = env => env.AddOrReplace("UseAutoMapperProfile", true)
            }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                HttpResponseMessage response = await testEnvironment.Server.BuildHttpClient(token)
                    .GetAsync("api/mapper/some-api");

                var json = JToken.Parse(await response.Content.ReadAsStringAsync());

                Assert.AreEqual("ValidUserName", json["UserId"].ToString());
                Assert.AreEqual("-1", json["Id"].ToString());
            }

            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs
            {
                ActiveAppEnvironmentCustomizer = env => env.AddOrReplace("UseAutoMapperProfile", true)
            }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                HttpResponseMessage response = await testEnvironment.Server.BuildHttpClient(token)
                    .GetAsync("api/mapper/some-api");

                var json = JToken.Parse(await response.Content.ReadAsStringAsync());

                Assert.AreEqual("ValidUserName", json["UserId"].ToString());
                Assert.AreEqual("-1", json["Id"].ToString());
            }
        }
    }
}
