using System;
using System.Diagnostics;

namespace Foundation.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Foundation Test";

            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs
            {
                UseRealServer = true,
                UseProxyBasedDependencyManager = false,
                UseSso = false
            }))
            {
                string url = null;

                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                url = $@"{testEnvironment.Server.Uri}SignIn#id_token=0&access_token={token.access_token}&token_type={token.token_type}&expires_in=86400&scope=openid%20profile%20user_info&state={{}}&session_state=0";

                Process.Start(url);

                Console.WriteLine(url);

                Console.WriteLine("Press any key to stop...");

                Console.ReadKey();
            }
        }
    }
}
