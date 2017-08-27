using Bit.Test;
using System;
using System.Diagnostics;
using System.IO;

namespace Bit.Tests
{
    public class Program
    {
        public static void Main()
        {
            Environment.CurrentDirectory = Path.Combine(Environment.CurrentDirectory, @"bin\debug\net461");

            Console.Title = "Bit Tests";

            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs
            {
                UseRealServer = true,
                UseProxyBasedDependencyManager = false,
                UseAspNetCore = true
            }))
            {

                Process.Start(testEnvironment.Server.Uri);

                Console.WriteLine(testEnvironment.Server.Uri);

                Console.WriteLine("Press any key to stop...");

                Console.ReadKey();
            }
        }
    }
}
