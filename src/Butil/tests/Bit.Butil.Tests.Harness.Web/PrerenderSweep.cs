using System.Linq.Expressions;
using System.Reflection;
using Bit.Butil;
using Microsoft.AspNetCore.Components;

namespace ButilTests.Harness.Web;

/// <summary>
/// Calls every public member of every Butil service with the <c>IJSRuntime</c> a static render hands out, and
/// records how each call went.
/// </summary>
/// <remarks>
/// Bit.Butil's contract for prerendering and static SSR is that nothing throws: reads answer with safe defaults,
/// writes and subscriptions are inert (see <c>InternalJSRuntimeExtensions.IsJsRuntimeInvalid</c>). That contract
/// is per call site - every one of the library's hundreds of methods has to route through the guarded invoke
/// path - so it is checked the only way that scales with the library: by calling all of them. A method added
/// tomorrow is swept tomorrow, with no list to update.
/// <br/>
/// Arguments are made up (see <see cref="TryCreateArgument"/>), so a member that validates them may refuse -
/// that is recorded as <see cref="PrerenderSweepOutcome.Rejected"/> and is not a failure. Anything else thrown,
/// or a call that never finishes, is.
/// </remarks>
public static class PrerenderSweep
{
    // Generous for a call that has nothing to wait on: every one of them should complete synchronously or
    // nearly so. What this catches is the call that awaits a JavaScript answer that can never come.
    private static readonly TimeSpan CallTimeout = TimeSpan.FromSeconds(5);

    private static readonly object NoValue = new();

    public static async Task<IReadOnlyList<PrerenderSweepEntry>> RunAsync(IServiceProvider services)
    {
        var entries = new List<PrerenderSweepEntry>();

        foreach (var type in ServiceTypes())
        {
            object service;
            try
            {
                service = services.GetRequiredService(type);
            }
            catch (Exception exception)
            {
                entries.Add(new(type.Name, "(resolve)", PrerenderSweepOutcome.Failed, Describe(exception)));
                continue;
            }

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetIndexParameters().Length > 0 || property.GetMethod is not { IsPublic: true }) continue;

                entries.Add(await CallAsync(type.Name, property.Name, () => property.GetValue(service)));
            }

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (method.IsSpecialName || method.DeclaringType == typeof(object)) continue;
                // Disposal is the scope's to do, after the render - and it runs then, so its failures still
                // surface, as errors in the server log the hosting suite reads.
                if (method.Name is nameof(IAsyncDisposable.DisposeAsync) or nameof(IDisposable.Dispose)) continue;

                var member = Describe(method);

                if (TryBind(method, out var callable, out var arguments, out var reason) is false)
                {
                    entries.Add(new(type.Name, member, PrerenderSweepOutcome.Skipped, reason));
                    continue;
                }

                entries.Add(await CallAsync(type.Name, member, () => callable.Invoke(service, arguments)));
            }
        }

        return entries;
    }

    private static IEnumerable<Type> ServiceTypes() =>
        typeof(BitButil).Assembly.GetTypes()
            .Where(type => type.GetCustomAttribute<ButilServiceAttribute>(inherit: false) is not null)
            .OrderBy(type => type.Name, StringComparer.Ordinal);

    private static async Task<PrerenderSweepEntry> CallAsync(string service, string member, Func<object?> call)
    {
        try
        {
            // Off the render's own thread, so a member that blocks synchronously is reported as hung instead of
            // stalling the whole response. Nothing in a prerender call needs the renderer's context.
            var pending = Task.Run(async () =>
            {
                var result = await AwaitResultAsync(Unwrap(call));
                await DisposeResultAsync(result);
            });

            await pending.WaitAsync(CallTimeout);
            return new(service, member, PrerenderSweepOutcome.Completed, null);
        }
        catch (TimeoutException)
        {
            return new(service, member, PrerenderSweepOutcome.Hung, $"did not complete within {CallTimeout.TotalSeconds}s");
        }
        // An exception type the library declares itself (a GeolocationException, say) is a failure mode it
        // documents on the member, chosen on purpose - not the accidental NullReferenceException or
        // InvalidOperationException out of a read the prerender guard should have covered.
        catch (Exception exception) when (exception is ArgumentException || exception.GetType().Assembly == typeof(BitButil).Assembly)
        {
            return new(service, member, PrerenderSweepOutcome.Rejected, Describe(exception));
        }
        catch (Exception exception)
        {
            return new(service, member, PrerenderSweepOutcome.Failed, Describe(exception));
        }
    }

    private static object? Unwrap(Func<object?> call)
    {
        try
        {
            return call();
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private static async Task<object?> AwaitResultAsync(object? result)
    {
        switch (result)
        {
            case Task task:
                await task;
                return ResultOf(task);
            case ValueTask valueTask:
                await valueTask;
                return null;
        }

        // ValueTask<T> has no non-generic base to match on.
        if (result?.GetType() is { IsGenericType: true } type && type.GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            var task = (Task)type.GetMethod(nameof(ValueTask<int>.AsTask))!.Invoke(result, null)!;
            await task;
            return ResultOf(task);
        }

        return result;
    }

    private static object? ResultOf(Task task) =>
        task.GetType() is { IsGenericType: true } type && type.GetProperty(nameof(Task<int>.Result)) is { } property && property.PropertyType.Name != "VoidTaskResult"
            ? property.GetValue(task)
            : null;

    private static async Task DisposeResultAsync(object? result)
    {
        // What a call hands back during prerender is inert, and disposing it has to be as well - a component
        // that prerendered keeps and disposes these like any other.
        switch (result)
        {
            case IAsyncDisposable asyncDisposable:
                await asyncDisposable.DisposeAsync();
                break;
            case IDisposable disposable:
                disposable.Dispose();
                break;
        }
    }

    private static bool TryBind(MethodInfo method, out MethodInfo callable, out object?[] arguments, out string reason)
    {
        callable = method;
        arguments = [];
        reason = string.Empty;

        if (method.IsGenericMethodDefinition)
        {
            var closed = CloseGeneric(method);
            if (closed is null)
            {
                reason = "no type argument satisfies the generic constraints";
                return false;
            }
            callable = closed;
        }

        var parameters = callable.GetParameters();
        arguments = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            if (TryCreateArgument(parameters[i], out var value) is false)
            {
                reason = $"cannot make a {parameters[i].ParameterType.Name} for '{parameters[i].Name}'";
                return false;
            }
            arguments[i] = value;
        }

        return true;
    }

    private static MethodInfo? CloseGeneric(MethodInfo method)
    {
        var candidates = method.GetGenericArguments().Select(TypeArgumentCandidates).ToArray();
        var attempts = candidates.Max(c => c.Length);

        // The n-th candidate of every parameter together; enough for the library's generic members, which
        // constrain a single parameter each.
        for (var n = 0; n < attempts; n++)
        {
            try
            {
                return method.MakeGenericMethod([.. candidates.Select(c => c[Math.Min(n, c.Length - 1)])]);
            }
            catch (ArgumentException)
            {
                // A constraint this combination does not satisfy; try the next.
            }
        }

        return null;
    }

    private static Type[] TypeArgumentCandidates(Type parameter)
    {
        var constraints = parameter.GetGenericParameterConstraints().Where(c => c != typeof(ValueType)).ToArray();

        // A parameter constrained to a type of the library's own (an algorithm description, say) gets the concrete
        // types that satisfy it, so the member is called with something it accepts instead of being skipped.
        if (constraints.Length > 0)
        {
            return [.. typeof(BitButil).Assembly.GetTypes()
                .Where(type => type.IsAbstract is false && type.IsGenericTypeDefinition is false)
                .Where(type => constraints.All(constraint => constraint.IsAssignableFrom(type)))
                .Where(type => type.IsValueType || type.GetConstructor(Type.EmptyTypes) is not null)
                .OrderBy(type => type.Name, StringComparer.Ordinal)
                .DefaultIfEmpty(typeof(object))];
        }

        // object first: it is what an unconstrained payload type is checked against at run time (an event
        // listener's argument type is matched to the event it listens for, and the sweep's made-up event name
        // maps to object), so any other first choice fails validation rather than exercising the call.
        return [typeof(object), typeof(string), typeof(int)];
    }

    private static bool TryCreateArgument(ParameterInfo parameter, out object? value)
    {
        var type = parameter.ParameterType;
        value = null;

        if (type.IsByRef || type.IsPointer) return false;

        if (parameter.HasDefaultValue && parameter.DefaultValue is not DBNull and not Missing)
        {
            value = parameter.DefaultValue ?? (type.IsValueType ? Activator.CreateInstance(type) : null);
            return true;
        }

        value = Sample(type);
        return ReferenceEquals(value, NoValue) is false;
    }

    /// <summary>A plausible value of <paramref name="type"/>, or <see cref="NoValue"/> when there is none to make.</summary>
    private static object? Sample(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is not null) return null;
        if (type == typeof(string) || type == typeof(object)) return "butil";
        if (type == typeof(bool)) return false;
        if (type == typeof(char)) return 'b';
        if (type.IsEnum) return Enum.GetValues(type).GetValue(0);
        if (type.IsPrimitive || type == typeof(decimal)) return Convert.ChangeType(1, type, System.Globalization.CultureInfo.InvariantCulture);
        if (type == typeof(CancellationToken)) return CancellationToken.None;
        if (type == typeof(TimeSpan)) return TimeSpan.FromMilliseconds(10);
        if (type == typeof(DateTime)) return DateTime.UtcNow;
        if (type == typeof(DateTimeOffset)) return DateTimeOffset.UtcNow;
        if (type == typeof(Guid)) return Guid.NewGuid();
        if (type == typeof(Uri)) return new Uri("https://example.com/");
        if (type == typeof(ElementReference)) return new ElementReference("butil-prerender-sweep");
        if (type == typeof(Stream)) return new MemoryStream([1, 2, 3]);

        if (typeof(Delegate).IsAssignableFrom(type)) return NoOpDelegate(type);

        if (type.IsArray)
        {
            var element = type.GetElementType()!;
            var sample = Sample(element);
            if (ReferenceEquals(sample, NoValue)) return Array.CreateInstance(element, 0);

            var array = Array.CreateInstance(element, 1);
            array.SetValue(sample, 0);
            return array;
        }

        if (type.IsGenericType && type.IsInterface)
        {
            var definition = type.GetGenericTypeDefinition();
            var typeArguments = type.GetGenericArguments();

            if (definition == typeof(IEnumerable<>) || definition == typeof(IReadOnlyList<>) || definition == typeof(IReadOnlyCollection<>) ||
                definition == typeof(IList<>) || definition == typeof(ICollection<>))
            {
                return Sample(typeArguments[0].MakeArrayType());
            }

            if (definition == typeof(IDictionary<,>) || definition == typeof(IReadOnlyDictionary<,>))
            {
                return Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeArguments));
            }
        }

        if (type.IsValueType) return Activator.CreateInstance(type);

        // Options and payload DTOs: a public parameterless constructor gives a default-shaped instance. Handles
        // (a WorkerHandle, a stream) have none on purpose - only the library makes those - so a member that
        // takes one is skipped rather than handed a fake.
        if (type.IsAbstract is false && type.GetConstructor(Type.EmptyTypes) is { } constructor)
        {
            return constructor.Invoke(null);
        }

        return NoValue;
    }

    private static Delegate NoOpDelegate(Type delegateType)
    {
        var invoke = delegateType.GetMethod("Invoke")!;
        var parameters = invoke.GetParameters().Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray();

        Expression body;
        var returnType = invoke.ReturnType;
        if (returnType == typeof(void)) body = Expression.Empty();
        else if (returnType == typeof(Task)) body = Expression.Constant(Task.CompletedTask);
        else if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var fromResult = typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(returnType.GetGenericArguments()[0]);
            body = Expression.Call(fromResult, Expression.Default(returnType.GetGenericArguments()[0]));
        }
        else body = Expression.Default(returnType);

        return Expression.Lambda(delegateType, body, parameters).Compile();
    }

    private static string Describe(MethodInfo method) =>
        $"{method.Name}({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name))})";

    private static string Describe(Exception exception) => $"{exception.GetType().Name}: {exception.Message}";
}
