using Bit.Core;
using Bit.Owin;
using Bit.Owin.Implementations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace CustomerDtoControllerSample
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            AssemblyContainer.Current.Init();

            AspNetCoreAppEnvironmentsProvider.Current.Use();

            await BuildWebHost(args)
                .RunAsync();
        }

        public static IHost BuildWebHost(string[] args) =>
            BitWebHost.CreateWebHost(args)
                .Build();
    }
}
