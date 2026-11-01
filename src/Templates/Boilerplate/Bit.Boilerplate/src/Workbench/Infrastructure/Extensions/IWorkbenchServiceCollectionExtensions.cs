using Bit.Bcrdt;
using Bit.Bcrdt.Server;
using Microsoft.Extensions.Localization;
//#if (module == "Admin")
using Boilerplate.Workbench.Components.Pages.AppPages.Dashboard;
//#endif

namespace Boilerplate.Workbench.Infrastructure.Extensions;

/// <summary>
/// Registers the Workbench's own services: the scenario, sample data, page stores, screenshots, Edit text mode and the review.
/// </summary>
public static class IWorkbenchServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public void AddWorkbenchServices(IConfiguration configuration)
        {
            services.AddScoped<WorkbenchScenarioState>();
            services.AddSingleton<WorkbenchScreenshotService>();
            services.AddSingleton(_ => new WorkbenchScreenshotLimiter(configuration));
            services.AddSingleton<WorkbenchSnapshots>();
            services.AddScoped<PosDashboardDataService>();
            services.AddScoped<BuildingsService>();
            services.AddScoped<AssistantService>();
            services.AddScoped<ShiftsService>();

            // The assistant page's chat runs on the sample replies of AssistantService.
            services.AddBitBlazorUIAIServices(ai =>
            {
                ai.UseTransport(sp => sp.GetRequiredService<AssistantService>());
                ai.ConfigureGenerativeUi(components =>
                {
                    components.Register<AssistantSalesCard>("sales_trend");
                    components.Register<AssistantStockCard>("low_stock");
                });
            });

            // The pages' stores, saved per view in IndexedDB, so a reload and a screenshot keep what was done on a page.
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

            // Every localizer records the texts it shows, so Edit text mode can trace a text on the page to its resource entry.
            services.AddSingleton<WorkbenchTextRecorder>();
            services.AddSingleton<WorkbenchTextEdits>();
            services.AddTransient(typeof(IStringLocalizer<>), typeof(WorkbenchRecordingLocalizer<>));

            // Inspect mode: each bit BlazorUI component is recorded as it's created, so a click in a view finds it, and its tag in the code takes parameter changes.
            services.AddSingleton<WorkbenchComponentInstances>();
            services.AddScoped<IComponentActivator, WorkbenchComponentActivator>();
            services.AddSingleton<WorkbenchComponentEdits>();

            // The review: a Bcrdt document kept in App_Data/Reviews, synced to every circuit through the relay and served to the agent over MCP.
            services.AddSingleton<WorkbenchReviewAgent>();
            services.AddSingleton<WorkbenchReviewAttachments>();
            services.AddSingleton<WorkbenchReviewRelay>();
            services.AddScoped<WorkbenchReviewSession>();
            services.AddBitBcrdtServices(collaboration => collaboration.UseTransport<WorkbenchReviewRelay>().UseInMemoryStore());
            services.AddBitBcrdtDocumentHost(sp => new()
            {
                Store = WorkbenchFolders.Project is null ? new BcrdtInMemoryDocumentStore() : new BcrdtFileSystemDocumentStore(WorkbenchFolders.Reviews),
                ValidateUpdate = context =>
                {
                    // Wakes an agent waiting for comments; only the review document is accepted.
                    sp.GetRequiredService<WorkbenchReviewAgent>().Pulse();
                    return ValueTask.FromResult(context.DocumentId == WorkbenchReviewDocument.Id);
                }
            });
            services.AddScoped<WorkbenchReviewTools>();
            services.AddMcpServer(options => options.ServerInfo = new() { Name = "workbench", Title = "bit Workbench", Version = "1.0.0" })
                .WithHttpTransport()
                .WithTools<WorkbenchReviewTools>();
        }
    }
}
