namespace Bit.Websites.Platform.Web;

public partial class Program
{
    public static async Task Main(string[] args)
    {
#if !BlazorWebAssembly && !BlazorServer
        throw new InvalidOperationException("Please switch to either Blazor WebAssembly or Server as described in readme.md");
#else
        await CreateHostBuilder(args)
            .RunAsync();
#endif
    }
}
