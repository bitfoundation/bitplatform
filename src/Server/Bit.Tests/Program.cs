using System;
using System.Diagnostics;
using Bit.Test;

namespace Bit.Tests
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.Title = "Bit Tests";

            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs
            {
                UseRealServer = true,
                UseProxyBasedDependencyManager = false
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
