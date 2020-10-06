using Autofac;
using Autofac.Core.Resolving.Pipeline;
using Bit.Core.Contracts;
using Bit.Core.Implementations;
using FakeItEasy;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Bit.Test.Implementations
{
    public class AutofacTestDependencyManager : AutofacDependencyManager, IResolveMiddleware
    {

        public override IDependencyManager Init()
        {
            base.Init();

            ContainerBuilder builder = GetContainerBuidler();

            builder.ComponentRegistryBuilder.Registered += (sender, args) =>
            {
                TypeInfo implementationType = args.ComponentRegistration.Activator.LimitType.GetTypeInfo();

                if (IsGoingToCreateProxyForImplementationType(implementationType))
                {
                    ConstructorInfo[] constructors = implementationType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                    if (constructors.Length == 1)
                    {
                        if (constructors.Single().GetParameters().Any())
                        {
                            throw new InvalidOperationException($"{implementationType.FullName} has only one constructor which has parameter");
                        }
                    }
                    else
                    {
                        if (constructors.All(c => !c.GetParameters().Any()))
                        {
                            throw new InvalidOperationException($"{implementationType.FullName} has more than one constructor, but all without parameter");
                        }

                        if (constructors.All(c => c.GetParameters().Any()))
                        {
                            throw new InvalidOperationException($"{implementationType.FullName} has more than one constructor, but all with parameter");
                        }

                        if (constructors.GroupBy(c => c.IsPublic).Count() == 1)
                        {
                            throw new InvalidOperationException($"{implementationType.FullName} has more than one constructor, all with same visibility level");
                        }

                        if (constructors.Single(c => !c.IsPublic).GetParameters().Any())
                        {
                            throw new InvalidOperationException($"{implementationType.FullName} has more than one constructor, and its non public constructor has parameter");
                        }
                    }

                    if (implementationType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly).Any(m => (m.IsPublic || m.IsFamily /*protected*/) && !m.IsVirtual))
                    {
                        throw new InvalidOperationException($"{implementationType.FullName} has non virtual public/protected instance members");
                    }
                }

                args.ComponentRegistration.PipelineBuilding += (sender, pipeline) =>
                {
                    pipeline.Use(this);
                };
            };

            return this;
        }

        public virtual List<Func<TypeInfo, bool>> AutoProxyCreationIncludeRules { get; set; } = new List<Func<TypeInfo, bool>> { };

        public virtual List<Func<TypeInfo, bool>> AutoProxyCreationIgnoreRules { get; set; } = new List<Func<TypeInfo, bool>> { };

        public virtual bool IsGoingToCreateProxyForImplementationType(TypeInfo implementationType)
        {
            if (implementationType == null)
                throw new ArgumentNullException(nameof(implementationType));

            return implementationType.IsClass
                && AutoProxyCreationIncludeRules.Any(rule => rule(implementationType) == true)
                && AutoProxyCreationIgnoreRules.All(rule => rule(implementationType) == false);
        }


        public virtual void Execute(ResolveRequestContext context, Action<ResolveRequestContext> next)
        { 
            next(context);

            object? instance = context.Instance;

            if (instance != null)
            {
                TypeInfo instanceType = instance.GetType().GetTypeInfo();

                if (IsGoingToCreateProxyForImplementationType(instanceType))
                {
                    instance = _createProxyForService.MakeGenericMethod(instanceType).Invoke(this, new[] { instance })!;
                    context.Instance = instance;
                }

                if (Objects is BlockingCollection<object> blockingCollection)
                    blockingCollection.Add(instance);
                else if (Objects is ICollection<object> collection)
                    collection.Add(instance);
                else
                    throw new NotSupportedException($"Provide compatible collection type for {nameof(Objects)}");
            }
        }

        private readonly MethodInfo _createProxyForService = typeof(AutofacTestDependencyManager).GetTypeInfo().GetMethod(nameof(CreateProxyForService))!;

        public virtual T CreateProxyForService<T>(object serviceInstance)
            where T : class
        {
            T originalInstance = (T)serviceInstance;

            serviceInstance = A.Fake<T>(x =>
            {
                x.Wrapping(originalInstance);
            });

            return (T)serviceInstance;
        }

        public virtual IEnumerable<object> Objects { get; } = new BlockingCollection<object> { };

        public virtual string ReportObjects
        {
            get { return string.Join(Environment.NewLine, Objects.Select(o => o.GetType().FullName).OrderBy(o => o)); }
        }

        public PipelinePhase Phase => PipelinePhase.Activation;
    }
}
