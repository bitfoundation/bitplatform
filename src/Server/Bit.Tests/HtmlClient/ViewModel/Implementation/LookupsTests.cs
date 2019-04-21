using Bit.Test;
using Bit.Test.Implementations;
using Bit.Test.Server;
using Bit.Tests.Api.ApiControllers;
using Bit.Tests.Model.Dto;
using FakeItEasy;
using IdentityModel.Client;
using Microsoft.AspNet.OData.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using System.Linq;
using System.Threading.Tasks;

namespace Bit.Tests.HtmlClient.ViewModel.Implementation
{
    [TestClass]
    public class LookupsTests
    {
        [TestMethod]
        [TestCategory("HtmlClient")]
        public virtual async Task TestODataLookupBaseFilter()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.BuildWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    await driver.NavigateToRoute("lookups-page");
                    await driver.WaitForCondition(d => d.Url.Contains("lookups-page"));
                }

                CountriesController countriesController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<CountriesController>()
                    .Last();

                A.CallTo(() => countriesController.GetAllCountries(A<ODataQueryOptions<CountryDto>>.That.Matches(query => query.Filter.RawValue == "((SomeProperty eq 1) or (SomeProperty eq 3))")))
                    .MustHaveHappenedOnceExactly();
            }
        }
    }
}
