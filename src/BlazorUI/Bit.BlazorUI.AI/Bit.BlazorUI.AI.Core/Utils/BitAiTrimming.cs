namespace Bit.BlazorUI;

/// <summary>
/// The messages of the <see cref="System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute"/> and
/// <see cref="System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute"/> the package puts on the members that read
/// an app's own types by reflection, so an app that trims (or compiles ahead of time) is told the same thing, and what to
/// use instead, wherever it calls one.
/// </summary>
internal static class BitAiTrimming
{
    /// <summary>A type of the app (de)serialized with the reflection-based System.Text.Json.</summary>
    internal const string JsonUnreferencedCode = "Reads a type of the app with the reflection-based System.Text.Json serializer, which needs members of that type that trimming can remove. Use the overload that takes a JsonTypeInfo<T> from the app's own JsonSerializerContext.";

    /// <summary>The native AOT side of <see cref="JsonUnreferencedCode"/>.</summary>
    internal const string JsonDynamicCode = "Reads a type of the app with the reflection-based System.Text.Json serializer, which can need code generated at run time that native AOT cannot make. Use the overload that takes a JsonTypeInfo<T> from the app's own JsonSerializerContext.";

    /// <summary>A generative UI component, whose parameter types are described and bound by reflection.</summary>
    internal const string GenerativeUnreferencedCode = "Generative UI describes the parameters of a component to the model, and binds the model's JSON onto them, by reflecting over the parameter types and every type they reach, whose members trimming can remove. Keep those types from being trimmed (for example with [DynamicDependency] or a TrimmerRootDescriptor).";

    /// <summary>The native AOT side of <see cref="GenerativeUnreferencedCode"/>.</summary>
    internal const string GenerativeDynamicCode = "Generative UI binds the model's JSON onto the parameters of a component with the reflection-based System.Text.Json serializer, which can need code generated at run time that native AOT cannot make.";
}
