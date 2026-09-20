using Bit.Butil;

namespace ButilTests.PublishFixture;

/// <summary>
/// The whole app. Two Bit.Butil classes are named and no others, which is what makes the module set a
/// publish should arrive at small enough to assert on by name.
/// </summary>
/// <remarks>
/// Injected through the non-generic <see cref="IServiceProvider.GetService(Type)"/>, the way Blazor fills an
/// <c>@inject</c> property: what a scan reads is the type reference, and going through the generic overload
/// would add an annotation that has nothing to do with what is being measured.
/// <br/>
/// The classes are chosen for what they prove rather than for what they do. <see cref="Clipboard"/> and
/// <see cref="Geolocation"/> each need one module of their own name; the scenarios that add
/// <c>LocalStorage</c> or <c>Window</c> to this list would be measuring the class-to-module map, which the
/// checks in ScriptScanning.cs already cover against ILLink's own answer. Here the question is only whether a
/// publish carries what was worked out into the output.
/// </remarks>
public static class Program
{
    /// <summary>The modules the two classes below need, before the manifest closes them over dependencies.</summary>
    public const string ExpectedModules = "clipboard,geolocation";

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddBitButilServices();

        var app = builder.Build();

        app.MapGet("/", (IServiceProvider services) =>
        {
            var clipboard = (Clipboard)services.GetRequiredService(typeof(Clipboard));
            var geolocation = (Geolocation)services.GetRequiredService(typeof(Geolocation));

            return $"{clipboard.GetType().Name} {geolocation.GetType().Name}";
        });

        app.Run();
    }
}
