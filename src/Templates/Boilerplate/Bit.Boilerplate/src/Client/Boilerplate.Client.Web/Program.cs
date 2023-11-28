//-:cnd:noEmit
namespace Boilerplate.Client.Web;

public partial class Program
{
    public static async Task Main(string[] args)
    {
#if !BlazorWebAssembly && !BlazorServer
        throw new InvalidOperationException("Please switch to either blazor web assembly or server as described in https://bitplatform.dev/templates/hosting-models");
#else
        await CreateHostBuilder(args)
            .RunAsync();
#endif
    }
}
