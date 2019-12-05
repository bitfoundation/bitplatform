using Bit.Test;
using System;
using System.Diagnostics;
using static System.Runtime.InteropServices.RuntimeInformation;
using static System.Runtime.InteropServices.OSPlatform;

namespace Bit.Tests
{
    public class Program
    {
        public static void Main()
        {
            Console.Title = "Bit Tests";

            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs
            {
                UseRealServer = true,
                UseProxyBasedDependencyManager = false,
                UseAspNetCore = true,
                FullUri = "http://*:80"
            }))
            {
                const string url = "http://localhost/";

                var browser = IsOSPlatform(Windows) ? new ProcessStartInfo("cmd", $"/c start {url}") :
                              IsOSPlatform(OSX) ? new ProcessStartInfo("open", url) :
                                                        new ProcessStartInfo("xdg-open", url); //linux, unix-like

                Process.Start(browser);

                Console.WriteLine(testEnvironment.Server.Uri);

                Console.WriteLine("Press any key to stop...");

                Console.ReadKey();
            }
        }
    }
}
