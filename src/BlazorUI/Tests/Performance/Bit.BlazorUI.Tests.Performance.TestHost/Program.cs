var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<Bit.BlazorUI.Tests.Performance.TestHost.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();

// Make Program class accessible for WebApplicationFactory
public partial class Program { }
