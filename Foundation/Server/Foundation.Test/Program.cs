using System;
using System.Diagnostics;

namespace Foundation.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Foundation Test";
            bool useSso = false;

            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs
            {
                UseRealServer = true,
                UseProxyBasedDependencyManager = false,
                UseSso = useSso
            }))
            {
                string url = null;

                if (useSso == false)
                {
                    OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                    url = $@"{testEnvironment.Server.Uri}SignIn#id_token=0&access_token={token.access_token}&token_type={token.token_type}&expires_in=86400&scope=openid profile user_info&state={{}}&session_state=0";
                }
                else
                {
                    url = testEnvironment.Server.Uri;
                }

                Process.Start(url);

                Console.WriteLine(url);

                Console.WriteLine("Press any key to stop...");

                Console.ReadKey();
            }
        }
    }
}
