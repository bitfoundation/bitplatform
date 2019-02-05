using Bit.Core.Extensions;
using Bit.Test;
using System;
using System.Diagnostics;

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
                if (!PlatformUtilities.IsRunningOnDotNetCore)
                    Process.Start("http://localhost/");

                Console.WriteLine(testEnvironment.Server.Uri);

                Console.WriteLine("Press any key to stop...");

                Console.ReadKey();
            }
        }
    }
}
