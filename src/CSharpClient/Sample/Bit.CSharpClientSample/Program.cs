using Bit.CSharpClientSample.Data;
using Bit.Data;
using Microsoft.AspNetCore.Hosting;

namespace Bit.CSharpClientSample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            BitEfCoreMigrationsOnlyWebHostBuilder<SampleDbContext>.BuildWebHost(args);
    }
}
