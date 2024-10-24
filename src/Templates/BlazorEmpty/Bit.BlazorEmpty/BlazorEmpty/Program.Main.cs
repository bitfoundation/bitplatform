using BlazorEmpty.Components;
#if (UseWebAssembly)
using BlazorEmpty.Client.Pages;
#endif

namespace BlazorEmpty;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        #if (!UseServer && !UseWebAssembly)
        builder.Services.AddRazorComponents();
        #else
        builder.Services.AddRazorComponents()
          #if (UseServer && UseWebAssembly)
                        .AddInteractiveServerComponents()
                        .AddInteractiveWebAssemblyComponents();
          #elif(UseServer)
                        .AddInteractiveServerComponents();
          #elif(UseWebAssembly)
                        .AddInteractiveWebAssemblyComponents();
          #endif
        #endif

        builder.Services.AddBitBlazorUIServices();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
#if (UseWebAssembly)
        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
        }
        else
#else
        if (!app.Environment.IsDevelopment())
#endif
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        if (builder.Environment.IsDevelopment() is false)
        {
            app.UseHttpsRedirection();
            app.UseResponseCompression();
        }

        app.UseStaticFiles();
        app.UseAntiforgery();
        //#if (Framework == 'net9.0')
        app.MapStaticAssets();
        //#endif
        #if (UseServer && UseWebAssembly)
        app.MapRazorComponents<App>()
           .AddInteractiveServerRenderMode()
           .AddInteractiveWebAssemblyRenderMode()
        #elif (UseServer)
        app.MapRazorComponents<App>()
           .AddInteractiveServerRenderMode();
        #elif (UseWebAssembly)
        app.MapRazorComponents<App>()
           .AddInteractiveWebAssemblyRenderMode()
        #else
        app.MapRazorComponents<App>();
        #endif
        #if (UseWebAssembly)
           .AddAdditionalAssemblies(typeof(BlazorEmpty.Client._Imports).Assembly);
        #endif

        app.Run();
    }
}
