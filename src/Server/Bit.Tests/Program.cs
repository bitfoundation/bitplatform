﻿using Bit.Test;
using System;
using System.Diagnostics;
using OSPlatform = System.Runtime.InteropServices.OSPlatform;
using static System.Runtime.InteropServices.RuntimeInformation;

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
                UseTestDependencyManager = false,
                FullUri = "http://*:80"
            }))
            {
                const string url = "http://localhost/";

                var browser = IsOSPlatform(OSPlatform.Windows) ? new ProcessStartInfo("cmd", $"/c start {url}") :
                              IsOSPlatform(OSPlatform.OSX) ? new ProcessStartInfo("open", url) :
                                                        new ProcessStartInfo("xdg-open", url); //linux, unix-like

                Process.Start(browser);

                Console.WriteLine(testEnvironment.Server.Uri);

                Console.WriteLine("Press any key to stop...");

                Console.ReadKey();
            }
        }
    }
}
