using System;
using System.Diagnostics;

namespace Foundation.Test
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.Title = "Foundation Test";

            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs
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
