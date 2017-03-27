using System;
using System.Diagnostics;

namespace IdentityServer.Test
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.Title = "Identity Server Test";

            using (IdentityServerTestEnvironment identityServerTestEnvironment = new IdentityServerTestEnvironment(useProxyBasedDependencyManager: false))
            {
                string url = $@"{identityServerTestEnvironment.Server.Uri}core/connect/authorize?scope=openid profile user_info&client_id=Test&redirect_uri=http://127.0.0.1/SignIn&response_type=id_token token&state={"{}"}&nonce=SgPoeilE1Tub";

                Process.Start(url);

                Console.WriteLine(url);

                Console.WriteLine("Press any key to stop...");

                Console.ReadKey();
            }
        }
    }
}
