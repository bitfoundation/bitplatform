using System;
using System.Diagnostics;
using System.Threading;
using Newtonsoft.Json;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;

namespace OpenQA.Selenium
{
    public static class IRemoteWebDriverExtensions
    {
        public static void NavigateToRoute(this RemoteWebDriver driver, string route)
        {
            Uri uri = new Uri(driver.Url);

            driver.Url = $"{uri.Scheme}://{uri.Host}:{uri.Port}/{route}";

            driver.GetElementById("testsConsole");
        }

        [DebuggerNonUserCode]
        public static IWebElement GetElementById(this RemoteWebDriver driver, string id)
        {
            new WebDriverWait(driver, TimeSpan.FromSeconds(2.5))
                .Until(ExpectedConditions.ElementExists(By.Id(id)));

            return driver.FindElementById(id);
        }

        [DebuggerNonUserCode]
        public static void ExecuteTest(this RemoteWebDriver driver, string testScript, params object[] args)
        {
            if (driver == null)
                throw new ArgumentNullException(nameof(driver));

            if (testScript == null)
                throw new ArgumentNullException(nameof(testScript));

            IWebElement testsConsole = driver.GetElementById("testsConsole");

            const string success = "Success";

            string consoleValue = testsConsole.GetAttribute("value");

            string arguments = JsonConvert.SerializeObject(args, new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });

            string finalTestScript = $"executeTest({testScript},'{arguments}');";

            driver.ExecuteScript(finalTestScript);

            new WebDriverWait(driver, TimeSpan.FromSeconds(10))
                .Until(d => testsConsole.GetAttribute("value") != consoleValue);

            consoleValue = testsConsole.GetAttribute("value");

            bool testIsPassed = consoleValue == success;

            Console.WriteLine("Test console value: " + consoleValue);
            Console.WriteLine("Url: " + driver.Url);
            Console.WriteLine("Final test script: " + finalTestScript);

            if (testIsPassed != true)
                throw new InvalidOperationException(consoleValue);
        }

        [DebuggerNonUserCode]
        public static void WaitForCondition(this RemoteWebDriver driver, Func<RemoteWebDriver, bool> condition)
        {
            new WebDriverWait(driver, TimeSpan.FromSeconds(2.5))
                .Until(d => condition)(driver);
        }
    }
}