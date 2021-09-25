using Bit.Core;
using Bit.Owin;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace WebApiSample
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            AssemblyContainer.Current.Init();

            await BuildWebHost(args)
                .RunAsync();
        }

        public static IHost BuildWebHost(string[] args) =>
            BitWebHost.CreateWebHost(args)
                .Build();
    }
}
