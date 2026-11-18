using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools;

/// <summary>
/// Static view of the DI container built from the <see cref="IServiceCollection"/> captured at registration time.
/// It shows what is registered and what implementation constructors ask for; it cannot observe runtime resolutions.
/// Instances and factory closures are never inspected, so configuration values and secrets are not exposed.
/// </summary>
public sealed class BevToolsServiceGraph
{
    private readonly Lazy<BevToolsDiagnostic[]> _findings;

    private BevToolsServiceGraph(List<BevToolsServiceInfo> services)
    {
        Services = services;
        _findings = new Lazy<BevToolsDiagnostic[]>(() => ComputeFindings().ToArray(), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public IReadOnlyList<BevToolsServiceInfo> Services { get; }

    public int Count => Services.Count;

    /// <summary>The findings of <see cref="Analyze"/>, computed once: the graph is immutable, and the Services panel reads them on every refresh.</summary>
    public IReadOnlyList<BevToolsDiagnostic> Findings => _findings.Value;

    public static BevToolsServiceGraph Build(IServiceCollection collection)
    {
        var services = new List<BevToolsServiceInfo>();
        var index = 0;
        foreach (var descriptor in collection)
        {
            Type? implementationType = null;
            string kind;
            if (descriptor.IsKeyedService)
            {
                implementationType = descriptor.KeyedImplementationType;
                kind = descriptor.KeyedImplementationInstance is not null ? "Instance" : descriptor.KeyedImplementationFactory is not null ? "Factory" : "Type";
            }
            else
            {
                implementationType = descriptor.ImplementationType;
                kind = descriptor.ImplementationInstance is not null ? "Instance" : descriptor.ImplementationFactory is not null ? "Factory" : "Type";
            }

            if (kind == "Instance")
            {
                implementationType = descriptor.IsKeyedService ? descriptor.KeyedImplementationInstance?.GetType() : descriptor.ImplementationInstance?.GetType();
            }

            var serviceType = descriptor.ServiceType;
            var ns = serviceType.Namespace ?? "";
            services.Add(new BevToolsServiceInfo
            {
                Index = index++,
                ServiceType = serviceType,
                ServiceTypeName = BevToolsTypeNames.Short(serviceType),
                Lifetime = descriptor.Lifetime,
                ImplementationType = implementationType,
                ImplementationName = implementationType is null ? kind.ToLowerInvariant() : BevToolsTypeNames.Short(implementationType) + (kind == "Type" ? "" : " (" + kind.ToLowerInvariant() + ")"),
                RegistrationKind = kind,
                IsKeyed = descriptor.IsKeyedService,
                Namespace = ns,
                IsFramework = ns.StartsWith("Microsoft.", StringComparison.Ordinal) || ns.StartsWith("System.", StringComparison.Ordinal),
            });
        }

        var byType = new Dictionary<Type, BevToolsServiceInfo>();
        foreach (var service in services)
        {
            if (service.IsKeyed)
            {
                continue;
            }

            if (byType.TryGetValue(service.ServiceType, out var previous))
            {
                previous.IsShadowed = true;
            }

            byType[service.ServiceType] = service;
        }

        foreach (var service in services)
        {
            if (service.RegistrationKind != "Type" || service.ImplementationType is null || service.IsKeyed)
            {
                continue;
            }

            var ctor = PickConstructor(service.ImplementationType, byType);
            if (ctor is null)
            {
                continue;
            }

            foreach (var parameter in ctor.GetParameters())
            {
                var dependency = Resolve(parameter.ParameterType, byType);
                if (dependency is null)
                {
                    if (!parameter.HasDefaultValue && !IsWellKnown(parameter.ParameterType))
                    {
                        service.UnresolvedDependencies.Add(BevToolsTypeNames.Short(parameter.ParameterType));
                    }

                    continue;
                }

                service.Dependencies.Add(dependency);
                dependency.Consumers.Add(service);
            }
        }

        foreach (var service in services)
        {
            service.SubtreeSize = CountSubtree(service, new HashSet<BevToolsServiceInfo>());
        }

        return new BevToolsServiceGraph(services);
    }

    /// <summary>
    /// Picks the constructor the container would pick: the greediest one whose parameters can all be satisfied.
    /// Choosing the greediest constructor outright produced dependency edges that never exist at runtime, and those
    /// edges fed an <c>Error</c>-severity captive-dependency finding — a false alarm is worse than a missing one.
    /// </summary>
    private static ConstructorInfo? PickConstructor(Type type, Dictionary<Type, BevToolsServiceInfo> byType)
    {
        if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition)
        {
            return null;
        }

        var ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        var marked = ctors.FirstOrDefault(c => c.IsDefined(typeof(ActivatorUtilitiesConstructorAttribute), false));
        if (marked is not null)
        {
            return marked;
        }

        ConstructorInfo? best = null;
        foreach (var ctor in ctors.OrderByDescending(c => c.GetParameters().Length))
        {
            best ??= ctor;
            if (ctor.GetParameters().All(p => p.HasDefaultValue || IsWellKnown(p.ParameterType) || Resolve(p.ParameterType, byType) is not null))
            {
                return ctor;
            }
        }

        return best;
    }

    private static BevToolsServiceInfo? Resolve(Type parameterType, Dictionary<Type, BevToolsServiceInfo> byType)
    {
        if (byType.TryGetValue(parameterType, out var direct))
        {
            return direct;
        }

        if (parameterType.IsGenericType)
        {
            var definition = parameterType.GetGenericTypeDefinition();
            if (definition == typeof(IEnumerable<>) || definition == typeof(Lazy<>) || definition == typeof(Microsoft.Extensions.Options.IOptions<>))
            {
                return Resolve(parameterType.GetGenericArguments()[0], byType) is { } inner && definition == typeof(IEnumerable<>) ? inner : byType.TryGetValue(definition, out var open) ? open : null;
            }

            if (byType.TryGetValue(definition, out var openGeneric))
            {
                return openGeneric;
            }
        }

        return null;
    }

    private static bool IsWellKnown(Type type) =>
        type == typeof(IServiceProvider) || type == typeof(string) || type.IsPrimitive || type.IsValueType
        || type.Namespace?.StartsWith("Microsoft.Extensions.Logging", StringComparison.Ordinal) == true
        || type.Namespace?.StartsWith("Microsoft.Extensions.Options", StringComparison.Ordinal) == true;

    private static int CountSubtree(BevToolsServiceInfo service, HashSet<BevToolsServiceInfo> visited)
    {
        if (!visited.Add(service))
        {
            return 0;
        }

        var count = 0;
        foreach (var dependency in service.Dependencies)
        {
            count += 1 + CountSubtree(dependency, visited);
        }

        return count;
    }

    /// <summary>Returns the dependency cycles found through constructor parameters.</summary>
    public IReadOnlyList<IReadOnlyList<BevToolsServiceInfo>> FindCycles()
    {
        var cycles = new List<IReadOnlyList<BevToolsServiceInfo>>();
        var seenCycleKeys = new HashSet<string>();
        var state = new Dictionary<BevToolsServiceInfo, int>();
        var stack = new List<BevToolsServiceInfo>();

        foreach (var service in Services)
        {
            Visit(service);
        }

        return cycles;

        void Visit(BevToolsServiceInfo service)
        {
            if (state.TryGetValue(service, out var s))
            {
                if (s == 1)
                {
                    var start = stack.IndexOf(service);
                    if (start >= 0)
                    {
                        var cycle = stack.Skip(start).ToList();
                        var key = string.Join(">", cycle.Select(c => c.Index).Order());
                        if (seenCycleKeys.Add(key))
                        {
                            cycles.Add(cycle);
                        }
                    }
                }

                return;
            }

            state[service] = 1;
            stack.Add(service);
            foreach (var dependency in service.Dependencies)
            {
                Visit(dependency);
            }

            stack.RemoveAt(stack.Count - 1);
            state[service] = 2;
        }
    }

    /// <summary>Evidence-based findings about registrations: captive dependencies, cycles, unusually large graphs, shadowed registrations.</summary>
    public IEnumerable<BevToolsDiagnostic> Analyze() => Findings;

    private IEnumerable<BevToolsDiagnostic> ComputeFindings()
    {
        foreach (var service in Services)
        {
            if (service.IsShadowed || service.IsFramework)
            {
                continue;
            }

            foreach (var dependency in service.Dependencies)
            {
                if (service.Lifetime == ServiceLifetime.Singleton && dependency.Lifetime != ServiceLifetime.Singleton)
                {
                    yield return new BevToolsDiagnostic(
                        "di.captive",
                        BevToolsDiagnosticSeverity.Error,
                        $"Singleton {service.ServiceTypeName} captures {dependency.Lifetime.ToString().ToLowerInvariant()} {dependency.ServiceTypeName}",
                        $"{service.ImplementationName} takes {dependency.ServiceTypeName} in its constructor. The singleton keeps the first {dependency.Lifetime.ToString().ToLowerInvariant()} instance forever, so per-circuit state leaks across users and disposed services are used.",
                        "Inject IServiceScopeFactory (or a factory) into the singleton, or make the dependency a singleton if it is stateless.",
                        Fingerprint: $"di.captive:{service.Index}:{dependency.Index}");
                }
                else if (service.Lifetime == ServiceLifetime.Scoped && dependency.Lifetime == ServiceLifetime.Transient && dependency.ImplementationType is not null && typeof(IDisposable).IsAssignableFrom(dependency.ImplementationType))
                {
                    yield return new BevToolsDiagnostic(
                        "di.transient-disposable",
                        BevToolsDiagnosticSeverity.Info,
                        $"{service.ServiceTypeName} holds transient disposable {dependency.ServiceTypeName}",
                        "Transient disposables resolved inside a scope are tracked by the scope until it ends; on Blazor Server that is the whole circuit lifetime.",
                        "Prefer scoped registration for disposable services on Blazor Server.",
                        Fingerprint: $"di.transient-disposable:{service.Index}:{dependency.Index}");
                }
            }

            if (service.Dependencies.Count >= 8)
            {
                yield return new BevToolsDiagnostic(
                    "di.large-graph",
                    BevToolsDiagnosticSeverity.Info,
                    $"{service.ServiceTypeName} has {service.Dependencies.Count} constructor dependencies",
                    "Dependencies: " + string.Join(", ", service.Dependencies.Select(d => d.ServiceTypeName)),
                    "Large constructors are a maintainability signal, not a bug; consider splitting responsibilities.",
                    Fingerprint: $"di.large-graph:{service.Index}");
            }
        }

        foreach (var cycle in FindCycles())
        {
            yield return new BevToolsDiagnostic(
                "di.cycle",
                BevToolsDiagnosticSeverity.Error,
                "Circular dependency: " + string.Join(" → ", cycle.Select(c => c.ServiceTypeName)) + " → " + cycle[0].ServiceTypeName,
                "Resolving any service in the cycle throws at runtime.",
                "Break the cycle with a Lazy<T>, a factory, or an event-based design.",
                Fingerprint: "di.cycle:" + string.Join(",", cycle.Select(c => c.Index)));
        }
    }
}
