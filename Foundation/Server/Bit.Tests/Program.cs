using IdentityModel.Client;
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
                string url = null;

                TokenResponse token = testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

                url = $@"{testEnvironment.Server.Uri}SignIn#id_token=0&access_token={token.AccessToken}&token_type={token.TokenType}&expires_in=86400&scope=openid profile user_info&state={{}}&session_state=0";

                Process.Start(url);

                Console.WriteLine(url);

                Console.WriteLine("Press any key to stop...");

                Console.ReadKey();
            }
        }
    }
}
