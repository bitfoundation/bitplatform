using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Threading;

namespace OpenQA.Selenium
{
    public static class IWebElementExtensions
    {
        [DebuggerNonUserCode]
        public static void WaitForControlPropertyEqual(this IWebElement element, string property, string value)
        {
            int triesCount = 0;

            do
            {
                try
                {
                    if (element.GetAttribute(property) == value)
                        return;
                }
                catch
                {
                    Thread.Sleep(250);
                    triesCount++;
                }
            } while (triesCount < 10);

            throw new InvalidOperationException($"property {property} has not value of {value}");
        }
    }

}

namespace OpenQA.Selenium.Remote
{
    public static class IRemoteWebDriverExtensions
    {
        public static void NavigateToRoute(this RemoteWebDriver driver, string route)
        {
            Uri uri = new Uri(driver.Url);

            driver.Url = $"{uri.Scheme}://{uri.Host}:{uri.Port}/{route}";

            driver.GetElementById("testsConsole");
        }

        public static void WaitForControlReady(this RemoteWebDriver driver)
        {
            Thread.Sleep(1500);
        }

        [DebuggerNonUserCode]
        public static IWebElement GetElementById(this RemoteWebDriver driver, string id)
        {
            int triesCount = 0;

            do
            {
                try
                {
                    IWebElement result = driver.FindElementById(id);

                    if (result != null)
                        return result;
                }
                catch
                {
                    Thread.Sleep(250);
                    triesCount++;
                }
            } while (triesCount < 10);

            throw new InvalidOperationException($"Element with id '{id}' couldn't be found.");
        }

        [DebuggerNonUserCode]
        public static void ExecuteTest(this RemoteWebDriver driver, string testScript, params object[] args)
        {
            if (driver == null)
                throw new ArgumentNullException(nameof(driver));

            if (testScript == null)
                throw new ArgumentNullException(nameof(testScript));

            IWebElement testsConsole = driver.GetElementById("testsConsole");

            string success = "Success";

            string consoleValue = (string)testsConsole.GetAttribute("value");

            string arguments = JsonConvert.SerializeObject(args, new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });

            string finalTestScript = $"executeTest({testScript},'{arguments}');";

            driver.WaitForControlReady();

            driver.ExecuteScript(finalTestScript);

            int triesCount = 0;

            do
            {
                if (testsConsole.GetAttribute("value") != consoleValue)
                    break;
                Thread.Sleep(200);
                triesCount++;
            } while (triesCount < 50);

            consoleValue = (string)testsConsole.GetAttribute("value");

            bool testIsPassed = consoleValue == success;

            Console.WriteLine("Test console value: " + consoleValue);
            Console.WriteLine("Url: " + driver.Url);
            Console.WriteLine("Final test script: " + finalTestScript);

            if (testIsPassed != true)
                throw new InvalidOperationException(consoleValue);
        }

        [DebuggerNonUserCode]
        public static void WaitForCondition(this RemoteWebDriver driver, Func<RemoteWebDriver, bool> func)
        {
            int triesCount = 0;

            do
            {
                try
                {
                    if ((func(driver) == true))
                        return;
                }
                catch { }
                finally
                {
                    Thread.Sleep(250);
                    triesCount++;
                }
            } while (triesCount < 10);

            throw new InvalidOperationException();
        }
    }
}
