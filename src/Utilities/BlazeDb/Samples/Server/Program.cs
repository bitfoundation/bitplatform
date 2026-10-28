using Bit.BlazeDb.Samples.Server;
using Bit.BlazeDb.Samples.Server.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// The pages render here - prerendered, then on Interactive Server circuits - so the database lives here too, in
// files, opened once for the process and disposed (its last flush included) when the host stops.
builder.Services.AddCoreServices<ServerSampleDatabase>();

var app = builder.Build();

if (app.Environment.IsDevelopment() is false)
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Bit.BlazeDb.Samples.Core._Imports).Assembly);

app.Run();
