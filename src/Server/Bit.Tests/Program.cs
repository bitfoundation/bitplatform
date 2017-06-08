using System;
using System.Diagnostics;
using Bit.Test;
using Bit.Core.Contracts;
using Bit.Owin.Middlewares;

namespace Bit.Tests
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.Title = "Bit Tests";

            BitOwinTestEnvironment testEnvironment = null;

            bool useASPNetCore = true;

            if (useASPNetCore == false)
            {
                testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs
                {
                    UseRealServer = true,
                    UseProxyBasedDependencyManager = false
                });
            }
            else
            {
                testEnvironment = new BitOwinCoreTestEnvironment(new TestEnvironmentArgs
                {
                    UseRealServer = true,
                    UseProxyBasedDependencyManager = false,
                    AdditionalDependencies = dependencyManager =>
                    {
                        dependencyManager.RegisterOwinMiddleware<RedirectToSsoIfNotLoggedInMiddlewareConfiguration>();
                        dependencyManager.RegisterDefaultPageMiddlewareUsingDefaultConfiguration();
                    }
                });
            }

            using (testEnvironment)
            {
                Process.Start(testEnvironment.Server.Uri);

                Console.WriteLine(testEnvironment.Server.Uri);

                Console.WriteLine("Press any key to stop...");

                Console.ReadKey();
            }
        }
    }
}
