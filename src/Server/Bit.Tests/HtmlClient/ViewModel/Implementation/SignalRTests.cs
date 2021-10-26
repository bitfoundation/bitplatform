﻿using Bit.Core.Contracts;
using Bit.Test;
using Bit.Test.Server;
using Bit.Http.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bit.Tests.HtmlClient.ViewModel.Implementation
{
    [TestClass]
    public class SignalRTests
    {
        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("SignalR")]
        public virtual async Task TestSignalRConnection()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (WebDriver driver = testEnvironment.Server.BuildWebDriver(new WebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest("testSignalRConnection");
                }

                IEnumerable<ILogger> loggers = testEnvironment.GetObjects<ILogger>()
                                    .ToList();

                Assert.IsTrue(loggers.SelectMany(l => l.LogData).Any(ld => ld.Key == nameof(IRequestInformationProvider.DisplayUrl) && ((string)ld.Value).Contains("signalr/start")));
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("SignalR")]
        public virtual async Task TestSignalRNoConnectionShouldBeMade()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (testEnvironment.Server.BuildWebDriver(new WebDriverOptions { Token = token }))
                {

                }

                Assert.IsFalse(testEnvironment.GetObjects<ILogger>()
                    .Any(logger => logger.LogData.Any(ld => ld.Key == nameof(IRequestInformationProvider.DisplayUrl) && ((string)ld.Value).Contains("SignalR/start"))));
            }
        }
    }
}
