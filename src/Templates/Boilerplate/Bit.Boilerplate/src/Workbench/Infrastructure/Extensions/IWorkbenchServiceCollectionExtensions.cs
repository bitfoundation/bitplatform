using Bit.Bcrdt;
using Bit.Bcrdt.Server;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;
//#if (module == "Admin")
using Boilerplate.Workbench.Components.Pages.AppPages.Dashboard;
//#endif

namespace Boilerplate.Workbench.Infrastructure.Extensions;

public static class IWorkbenchServiceCollectionExtensions
{
    public const string ScreenshotsRateLimitPolicy = "screenshots";

    extension(IServiceCollection services)
    {
        public void AddWorkbenchServices(IConfiguration configuration)
        {
            services.AddScoped<WorkbenchScenarioState>();
            services.AddSingleton<WorkbenchScreenshotService>();
            services.AddSingleton<WorkbenchSnapshots>();
            services.AddScoped<PosDashboardDataService>();
            services.AddScoped<BuildingsService>();
            services.AddScoped<AssistantService>();
            services.AddScoped<ShiftsService>();

            services.AddBitBlazorUIAIServices(ai =>
            {
                ai.UseTransport(sp => sp.GetRequiredService<AssistantService>());
                ai.ConfigureGenerativeUi(components =>
                {
                    components.Register<AssistantSalesCard>("sales_trend");
                    components.Register<AssistantStockCard>("low_stock");
                });
            });

            services.AddBitBlexServices(options => options.DevToolsName = "bit Workbench");
            services.AddBitBlexStore<WorkbenchStore>();
            services.AddBitBlexStore<ChromeStore>();
            services.AddBitBlexStore<BuildingsStore>();
            services.AddBitBlexStore<PosDashboardStore>();
            services.AddBitBlexStore<AssistantStore>();
            services.AddBitBlexStore<ShiftsStore>();
            services.AddBitBlexStore<SettingsStore>();
            //#if (module == "Admin")
            services.AddBitBlexStore<DashboardStore>();
            //#endif
            services.AddScoped<IBlexStorage, WorkbenchStateStorage>();
            services.AddBitBlexPersistence();
            services.AddScoped<IBitAccentColorStore, ChromeAccentStore>();
            services.AddAuthorizationCore(options => options.AddPolicy(WorkbenchPolicies.ManageBuildings, policy => policy.RequireRole(WorkbenchPolicies.AdminRoles)));

            services.AddSingleton<WorkbenchTextRecorder>();
            services.AddSingleton<WorkbenchTextEdits>();
            services.AddTransient(typeof(IStringLocalizer<>), typeof(WorkbenchRecordingLocalizer<>));

            services.AddSingleton<WorkbenchReviewAgent>();
            services.AddSingleton<WorkbenchReviewAttachments>();
            services.AddSingleton<WorkbenchReviewRelay>();
            services.AddScoped<WorkbenchReviewSession>();
            services.AddBitBcrdtServices(collaboration => collaboration.UseTransport<WorkbenchReviewRelay>().UseInMemoryStore());
            services.AddBitBcrdtDocumentHost(sp => new()
            {
                Store = new BcrdtFileSystemDocumentStore(WorkbenchFolders.Reviews),
                ValidateUpdate = context =>
                {
                    sp.GetRequiredService<WorkbenchReviewAgent>().Pulse();
                    return ValueTask.FromResult(context.DocumentId == WorkbenchReviewDocument.Id);
                }
            });
            services.AddScoped<WorkbenchReviewTools>();
            services.AddMcpServer(options => options.ServerInfo = new() { Name = "workbench", Title = "bit Workbench", Version = "1.0.0" })
                .WithHttpTransport()
                .WithTools<WorkbenchReviewTools>();

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddPolicy(ScreenshotsRateLimitPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = configuration.GetValue("Screenshots:PermitsPerMinutePerClient", 30),
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
            });
        }
    }
}
