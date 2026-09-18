using Microsoft.AspNetCore.Components;

// ResourceManager reads it at runtime to skip the satellite lookup for the neutral culture
[assembly: System.Resources.NeutralResourcesLanguage("en")]

namespace Bit.Minifier.Tests.Library
{
    /// <summary>An embedded resource is named after it: ResourceManager and IStringLocalizer find it that way.</summary>
    internal static class Glossary
    {
        public static int Count() => 1;
    }

    /// <summary>Found through Type.GetType by its name, as plugins and serializers do.</summary>
    public class Plugin
    {
        public string Hello(string greeting) => greeting + " from plugin";
    }

    /// <summary>A Blazor component: a prerendering server names it, and the types of its parameters.</summary>
    public class Widget : IComponent
    {
        [Parameter]
        public Palette? Colors { get; set; }
    }

    public class Palette
    {
        // NullabilityInfoContext reads [AllowNull]: the setter takes null, the getter never returns it
        [System.Diagnostics.CodeAnalysis.AllowNull]
        public string Name { get; set => field = value ?? ""; } = "";
    }

    /// <summary>System.Text.Json reads [SetsRequiredMembers] to tell that this constructor sets Code.</summary>
    public class Ticket
    {
        [System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
        public Ticket() => Code = "none";

        public required string Code { get; set; }
    }

    /// <summary>An app's entry point: the WebAssembly host finds the async Main through the wrapper's name.</summary>
    public static class Startup
    {
        public static async Task<int> Main(string[] args)
        {
            await Task.Yield();
            return args.Length;
        }

        public static int Wrapper(string[] args) => Main(args).GetAwaiter().GetResult();
    }
}

namespace Bit.Minifier.Tests.Library
{
    /// <summary>Constants that code lists by reflection, like a catalog of features or permissions.</summary>
    public static class Denominations
    {
        public const string Florin = "fl";
    }

    /// <summary>A server reports exceptions by type name; the client maps them back.</summary>
    public class AbacusException(string message) : Exception(message)
    {
    }

    /// <summary>A reflection-based HTTP client (Refit) builds requests from these parameter names.</summary>
    public interface IAbacus
    {
        int Multiply(int multiplicand);
    }

    public class Ledger
    {
        public string? Memo { get; set; }

        // Newtonsoft.Json asks this, by name, whether to write Memo
        public bool ShouldSerializeMemo() => Memo is not null;
    }
}

// a namespace no string names
namespace Bit.Minifier.Tests.Shelf
{
    public class Crate
    {
        public int Count(int rows, int columns) => new Pallet().Stack(rows, columns);
    }

    // renamed at every aggressive level; only super aggressive may take its namespace
    internal sealed class Pallet
    {
        public int Stack(int rows, int columns) => rows * columns;
    }
}

// stand-ins for Microsoft.AspNetCore.Components, which the minifier recognizes by name
namespace Microsoft.AspNetCore.Components
{
    public interface IComponent
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ParameterAttribute : Attribute
    {
    }
}
