using System.Diagnostics.CodeAnalysis;
#if NET9_0_OR_GREATER
using System.Text.Json.Schema;
#endif
using System.Text.Json.Serialization.Metadata;

namespace Bit.BlazorUI;

/// <summary>
/// Creates tools from delegates, with argument schemas derived from the argument type.
/// </summary>
/// <remarks>
/// <para>
/// Writing an <see cref="IBitAiTool"/> class for a one-line capability is friction that discourages
/// good tool design. These factories let a tool be declared where it is registered, while still
/// producing a real JSON Schema for the model.
/// </para>
/// <para>
/// Deriving a schema needs the JSON Schema exporter of System.Text.Json, which .NET 9 introduced: on .NET 8 every
/// member that derives one throws a <see cref="PlatformNotSupportedException"/>. There, set
/// <see cref="BitAiToolDefinition.ParametersSchema"/> yourself and create the tool with
/// <see cref="Create(BitAiToolDefinition, Func{BitAiToolInvocation, CancellationToken, ValueTask{BitAiToolOutcome}})"/>.
/// </para>
/// <para>
/// The overloads without a <see cref="JsonTypeInfo{T}"/> read the argument and result types by reflection, which an app
/// that is trimmed or compiled ahead of time cannot rely on; the ones that take a <see cref="JsonTypeInfo{T}"/> from the
/// app's own <see cref="System.Text.Json.Serialization.JsonSerializerContext"/> do not.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddBitBlazorUIAIServices(ai =&gt; ai.AddTool(BitAiTool.Create(
///     "get_weather",
///     "Look up the current weather for a city.",
///     async (WeatherArgs args, CancellationToken ct) =&gt; await weather.GetAsync(args.City, ct))));
/// </code>
/// </example>
public static class BitAiTool
{
    /// <summary>
    /// Creates a tool from a typed delegate, deriving the argument schema from <typeparamref name="TArgs"/>.
    /// </summary>
    /// <typeparam name="TArgs">The argument model. Its JSON Schema is sent to the model.</typeparam>
    /// <typeparam name="TResult">The result model, serialized back to the model as JSON.</typeparam>
    /// <param name="name">The identifier the model uses to call the tool.</param>
    /// <param name="description">What the tool does. The model reads this when deciding to call it.</param>
    /// <param name="handler">The implementation.</param>
    /// <param name="configure">Adjusts the generated definition, for example to require approval.</param>
    /// <returns>The tool.</returns>
    /// <exception cref="PlatformNotSupportedException">On .NET 8, which cannot derive the schema.</exception>
    [RequiresUnreferencedCode(BitAiTrimming.JsonUnreferencedCode)]
    [RequiresDynamicCode(BitAiTrimming.JsonDynamicCode)]
    public static IBitAiTool Create<TArgs, TResult>(
        string name,
        string description,
        Func<TArgs, CancellationToken, ValueTask<TResult>> handler,
        Func<BitAiToolDefinition, BitAiToolDefinition>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var definition = new BitAiToolDefinition(name)
        {
            Description = description,
            ParametersSchema = CreateSchema<TArgs>(),
        };

        if (configure is not null) definition = configure(definition);

        return new DelegateAiTool(definition, async (invocation, cancellationToken) =>
        {
            var args = invocation.GetArguments<TArgs>()
                ?? throw new InvalidOperationException($"Tool '{name}' received arguments that do not match {typeof(TArgs).Name}.");
            var result = await handler(args, cancellationToken).ConfigureAwait(false);
            return BitAiToolOutcome.Success(result);
        });
    }

    /// <summary>
    /// Creates a tool from a typed delegate, deriving the argument schema from <typeparamref name="TArgs"/> through the
    /// JSON metadata of the app's own <see cref="System.Text.Json.Serialization.JsonSerializerContext"/>, so it is safe
    /// to trim.
    /// </summary>
    /// <typeparam name="TArgs">The argument model. Its JSON Schema is sent to the model.</typeparam>
    /// <typeparam name="TResult">The result model, serialized back to the model as JSON.</typeparam>
    /// <param name="name">The identifier the model uses to call the tool.</param>
    /// <param name="description">What the tool does. The model reads this when deciding to call it.</param>
    /// <param name="argumentsTypeInfo">
    /// The metadata of <typeparamref name="TArgs"/>. The schema and the reading of the arguments both follow its
    /// options, so the property names the model is told are the ones that are read back.
    /// </param>
    /// <param name="resultTypeInfo">The metadata the result is serialized with.</param>
    /// <param name="handler">The implementation.</param>
    /// <param name="configure">Adjusts the generated definition, for example to require approval.</param>
    /// <returns>The tool.</returns>
    /// <exception cref="PlatformNotSupportedException">On .NET 8, which cannot derive the schema.</exception>
    public static IBitAiTool Create<TArgs, TResult>(
        string name,
        string description,
        JsonTypeInfo<TArgs> argumentsTypeInfo,
        JsonTypeInfo<TResult> resultTypeInfo,
        Func<TArgs, CancellationToken, ValueTask<TResult>> handler,
        Func<BitAiToolDefinition, BitAiToolDefinition>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(argumentsTypeInfo);
        ArgumentNullException.ThrowIfNull(resultTypeInfo);
        ArgumentNullException.ThrowIfNull(handler);

        var definition = new BitAiToolDefinition(name)
        {
            Description = description,
            ParametersSchema = CreateSchema(argumentsTypeInfo),
        };

        if (configure is not null) definition = configure(definition);

        return new DelegateAiTool(definition, async (invocation, cancellationToken) =>
        {
            var args = invocation.GetArguments(argumentsTypeInfo)
                ?? throw new InvalidOperationException($"Tool '{name}' received arguments that do not match {typeof(TArgs).Name}.");
            var result = await handler(args, cancellationToken).ConfigureAwait(false);
            return BitAiToolOutcome.Success(result, resultTypeInfo);
        });
    }

    /// <summary>
    /// Creates a tool from a delegate that returns a full <see cref="BitAiToolOutcome"/>, for tools that
    /// need to attach citations, custom display text, or a structured failure.
    /// </summary>
    /// <typeparam name="TArgs">The argument model.</typeparam>
    /// <param name="name">The identifier the model uses to call the tool.</param>
    /// <param name="description">What the tool does.</param>
    /// <param name="handler">The implementation.</param>
    /// <param name="configure">Adjusts the generated definition.</param>
    /// <returns>The tool.</returns>
    /// <exception cref="PlatformNotSupportedException">On .NET 8, which cannot derive the schema.</exception>
    [RequiresUnreferencedCode(BitAiTrimming.JsonUnreferencedCode)]
    [RequiresDynamicCode(BitAiTrimming.JsonDynamicCode)]
    public static IBitAiTool Create<TArgs>(
        string name,
        string description,
        Func<TArgs, BitAiToolInvocation, CancellationToken, ValueTask<BitAiToolOutcome>> handler,
        Func<BitAiToolDefinition, BitAiToolDefinition>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var definition = new BitAiToolDefinition(name)
        {
            Description = description,
            ParametersSchema = CreateSchema<TArgs>(),
        };

        if (configure is not null) definition = configure(definition);

        return new DelegateAiTool(definition, async (invocation, cancellationToken) =>
        {
            var args = invocation.GetArguments<TArgs>()
                ?? throw new InvalidOperationException($"Tool '{name}' received arguments that do not match {typeof(TArgs).Name}.");
            return await handler(args, invocation, cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>
    /// Creates a tool from a delegate that returns a full <see cref="BitAiToolOutcome"/>, deriving the argument schema
    /// through the JSON metadata of the app's own <see cref="System.Text.Json.Serialization.JsonSerializerContext"/>, so
    /// it is safe to trim.
    /// </summary>
    /// <typeparam name="TArgs">The argument model.</typeparam>
    /// <param name="name">The identifier the model uses to call the tool.</param>
    /// <param name="description">What the tool does.</param>
    /// <param name="argumentsTypeInfo">
    /// The metadata of <typeparamref name="TArgs"/>. The schema and the reading of the arguments both follow its
    /// options, so the property names the model is told are the ones that are read back.
    /// </param>
    /// <param name="handler">The implementation.</param>
    /// <param name="configure">Adjusts the generated definition.</param>
    /// <returns>The tool.</returns>
    /// <exception cref="PlatformNotSupportedException">On .NET 8, which cannot derive the schema.</exception>
    public static IBitAiTool Create<TArgs>(
        string name,
        string description,
        JsonTypeInfo<TArgs> argumentsTypeInfo,
        Func<TArgs, BitAiToolInvocation, CancellationToken, ValueTask<BitAiToolOutcome>> handler,
        Func<BitAiToolDefinition, BitAiToolDefinition>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(argumentsTypeInfo);
        ArgumentNullException.ThrowIfNull(handler);

        var definition = new BitAiToolDefinition(name)
        {
            Description = description,
            ParametersSchema = CreateSchema(argumentsTypeInfo),
        };

        if (configure is not null) definition = configure(definition);

        return new DelegateAiTool(definition, async (invocation, cancellationToken) =>
        {
            var args = invocation.GetArguments(argumentsTypeInfo)
                ?? throw new InvalidOperationException($"Tool '{name}' received arguments that do not match {typeof(TArgs).Name}.");
            return await handler(args, invocation, cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>
    /// Creates a tool from a raw handler, for tools whose arguments are dynamic.
    /// </summary>
    /// <param name="definition">The tool definition, including its schema.</param>
    /// <param name="handler">The implementation.</param>
    /// <returns>The tool.</returns>
    /// <remarks>
    /// Nothing is derived here: the schema is whatever <see cref="BitAiToolDefinition.ParametersSchema"/> holds. This is
    /// the overload to use on .NET 8, and anywhere the schema is written by hand.
    /// </remarks>
    public static IBitAiTool Create(
        BitAiToolDefinition definition,
        Func<BitAiToolInvocation, CancellationToken, ValueTask<BitAiToolOutcome>> handler) =>
        new DelegateAiTool(definition, handler);

    /// <summary>
    /// Produces the JSON Schema for a type, which is what a provider needs to constrain arguments.
    /// </summary>
    /// <typeparam name="T">The type to describe.</typeparam>
    /// <returns>The schema.</returns>
    /// <remarks>
    /// The type is read by reflection with the web defaults of System.Text.Json (camelCase property names). Not
    /// available on .NET 8, which has no JSON Schema exporter: pass the schema yourself there.
    /// </remarks>
    /// <exception cref="PlatformNotSupportedException">On .NET 8.</exception>
    [RequiresUnreferencedCode(BitAiTrimming.JsonUnreferencedCode)]
    [RequiresDynamicCode(BitAiTrimming.JsonDynamicCode)]
    public static JsonElement CreateSchema<T>() => CreateSchema(typeof(T));

    /// <summary>
    /// Produces the JSON Schema for a type.
    /// </summary>
    /// <param name="type">The type to describe.</param>
    /// <returns>The schema.</returns>
    /// <remarks>
    /// The type is read by reflection with the web defaults of System.Text.Json (camelCase property names). Not
    /// available on .NET 8, which has no JSON Schema exporter: pass the schema yourself there.
    /// </remarks>
    /// <exception cref="PlatformNotSupportedException">On .NET 8.</exception>
    [RequiresUnreferencedCode(BitAiTrimming.JsonUnreferencedCode)]
    [RequiresDynamicCode(BitAiTrimming.JsonDynamicCode)]
    public static JsonElement CreateSchema(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
#if NET9_0_OR_GREATER
        return CreateSchema(ReflectionSchemaOptions.GetTypeInfo(type));
#else
        throw SchemaNotSupported();
#endif
    }

    /// <summary>
    /// Produces the JSON Schema for a type from its JSON metadata, so it is safe to trim.
    /// </summary>
    /// <typeparam name="T">The type to describe.</typeparam>
    /// <param name="typeInfo">The metadata of <typeparamref name="T"/>, from the app's own JsonSerializerContext.</param>
    /// <returns>The schema.</returns>
    /// <remarks>
    /// The schema follows the options of <paramref name="typeInfo"/> (property naming, number handling, ...). Not
    /// available on .NET 8, which has no JSON Schema exporter: pass the schema yourself there.
    /// </remarks>
    /// <exception cref="PlatformNotSupportedException">On .NET 8.</exception>
    public static JsonElement CreateSchema<T>(JsonTypeInfo<T> typeInfo) => CreateSchema((JsonTypeInfo)typeInfo);

    /// <summary>
    /// Produces the JSON Schema for a type from its JSON metadata, so it is safe to trim.
    /// </summary>
    /// <param name="typeInfo">The metadata of the type, from the app's own JsonSerializerContext.</param>
    /// <returns>The schema.</returns>
    /// <remarks>
    /// The schema follows the options of <paramref name="typeInfo"/> (property naming, number handling, ...). Not
    /// available on .NET 8, which has no JSON Schema exporter: pass the schema yourself there.
    /// </remarks>
    /// <exception cref="PlatformNotSupportedException">On .NET 8.</exception>
    public static JsonElement CreateSchema(JsonTypeInfo typeInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);
#if NET9_0_OR_GREATER
        var node = typeInfo.GetJsonSchemaAsNode(new JsonSchemaExporterOptions
        {
            TreatNullObliviousAsNonNullable = true,
        });
        return JsonSerializer.SerializeToElement(node, BitAiJsonContext.Default.JsonNode);
#else
        throw SchemaNotSupported();
#endif
    }

#if NET9_0_OR_GREATER
    private static JsonSerializerOptions? _reflectionSchemaOptions;

    /// <remarks>
    /// The schema exporter refuses to run without a type-info resolver, and the implicit reflection
    /// default is not applied to an options instance built this way, so it is set explicitly here.
    /// Created on first use, inside the members that already say they reflect.
    /// </remarks>
    private static JsonSerializerOptions ReflectionSchemaOptions
    {
        [RequiresUnreferencedCode(BitAiTrimming.JsonUnreferencedCode)]
        [RequiresDynamicCode(BitAiTrimming.JsonDynamicCode)]
        get => _reflectionSchemaOptions ??= new(JsonSerializerDefaults.Web)
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        };
    }
#else
    private static PlatformNotSupportedException SchemaNotSupported() => new(
        "Deriving a JSON Schema from a type needs System.Text.Json's JsonSchemaExporter, which .NET 8 does not have. " +
        "On .NET 8, set BitAiToolDefinition.ParametersSchema yourself and create the tool with " +
        "BitAiTool.Create(BitAiToolDefinition, handler), reading the arguments with BitAiToolInvocation.GetArguments.");
#endif

    private sealed class DelegateAiTool(
        BitAiToolDefinition definition,
        Func<BitAiToolInvocation, CancellationToken, ValueTask<BitAiToolOutcome>> handler) : IBitAiTool
    {
        public BitAiToolDefinition Definition { get; } = definition;

        public ValueTask<BitAiToolOutcome> InvokeAsync(BitAiToolInvocation invocation, CancellationToken cancellationToken = default) =>
            handler(invocation, cancellationToken);
    }
}
