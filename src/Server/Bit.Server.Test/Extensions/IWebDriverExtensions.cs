﻿using Bit.Core.Implementations;
using Newtonsoft.Json;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace OpenQA.Selenium
{
    public static class IWebDriverExtensions
    {
        public static Task NavigateToRoute(this WebDriver driver, string route)
        {
            if (driver == null)
                throw new ArgumentNullException(nameof(driver));

            Uri uri = new Uri(driver.Url);

            driver.Url = $"{uri.Scheme}://{uri.Host}:{uri.Port}/{route}";

            driver.GetElementById("testsConsole");

            return Task.Delay(2500);
        }

        public static IWebElement FindElementByName(this WebDriver driver, string name)
        {
            return driver.FindElement(By.Name(name));
        }

        [DebuggerNonUserCode]
        public static IWebElement GetElementById(this WebDriver driver, string id)
        {
            if (driver == null)
                throw new ArgumentNullException(nameof(driver));

            new WebDriverWait(driver, TimeSpan.FromSeconds(2.5))
                .Until(driver => driver.FindElement(By.Id(id)) != null);

            return driver.FindElement(By.Id(id));
        }

        [DebuggerNonUserCode]
        public static async Task ExecuteTest(this WebDriver driver, string testScript, params object[] args)
        {
            if (driver == null)
                throw new ArgumentNullException(nameof(driver));

            if (testScript == null)
                throw new ArgumentNullException(nameof(testScript));

            if (args == null)
                throw new ArgumentNullException(nameof(args));

            IWebElement testsConsole = driver.GetElementById("testsConsole");

            const string success = "Success";

            string consoleValue = testsConsole.GetAttribute("value");

            string arguments = JsonConvert.SerializeObject(args, DefaultJsonContentFormatter.SerializeSettings());

            string finalTestScript = $"executeTest({testScript},'{arguments}');";

            driver.ExecuteScript(finalTestScript);

            string value = consoleValue;
            await driver.WaitForCondition(d => testsConsole.GetAttribute("value") != value).ConfigureAwait(false);

            consoleValue = testsConsole.GetAttribute("value");

            bool testIsPassed = consoleValue == success;

            Console.WriteLine("Test console value: " + consoleValue);
            Console.WriteLine("Url: " + driver.Url);
            Console.WriteLine("Final test script: " + finalTestScript);

            if (testIsPassed != true)
                throw new InvalidOperationException(consoleValue);
        }

        [DebuggerNonUserCode]
        public static async Task WaitForCondition(this WebDriver driver, Func<WebDriver, bool> condition)
        {
            if (driver == null)
                throw new ArgumentNullException(nameof(driver));

            if (condition == null)
                throw new ArgumentNullException(nameof(condition));

            int triesCount = 0;

            do
            {
                try
                {
                    if ((condition(driver) == true))
                        return;
                }
                catch { }
                finally
                {
                    await Task.Delay(250).ConfigureAwait(false);
                    triesCount++;
                }
            } while (triesCount < 20);

            throw new InvalidOperationException();
        }
    }
}
