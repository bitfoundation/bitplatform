using System.Linq;
using Bit.Test;
using Bit.Test.Core.Implementations;
using Bit.Test.Server;
using Bit.Tests.Api.ApiControllers;
using FakeItEasy;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using System.Web.OData.Query;
using Bit.Tests.Model.Dto;
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

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.NavigateToRoute("lookups-page");
                    driver.WaitForCondition(d => d.Url.Contains("lookups-page"));
                    await Task.Delay(5000);
                }

                CountriesController countriesController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<CountriesController>()
                    .Last();

                A.CallTo(() => countriesController.GetAllContries(A<ODataQueryOptions<CountryDto>>.That.Matches(query => query.Filter.RawValue == "((SomeProperty eq 1) or (SomeProperty eq 3))")))
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }
    }
}
