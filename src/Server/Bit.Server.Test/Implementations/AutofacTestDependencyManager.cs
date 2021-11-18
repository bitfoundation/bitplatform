using Autofac;
using Autofac.Core;
using Autofac.Core.Registration;
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
    public class TestMiddlewareSource : IServiceMiddlewareSource
    {
        public void ProvideMiddleware(Service service, IComponentRegistryServices availableServices, IResolvePipelineBuilder pipelineBuilder)
        {
            pipelineBuilder.Use(PipelinePhase.ServicePipelineEnd, (context, next) =>
            {
                next(context);

                object? instance = context.Instance;

                if (instance != null)
                {
                    TypeInfo instanceType = instance.GetType().GetTypeInfo();

                    if (TestDependencyManager.CurrentTestDependencyManager.IsGoingToCreateProxyForImplementationType(instanceType))
                    {
                        instance = _createProxyForService.MakeGenericMethod(instanceType).Invoke(this, new[] { instance })!;
                        context.Instance = instance;
                    }

                    TestDependencyManager.CurrentTestDependencyManager.Objects.Add(instance);
                }
            });
        }

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

        private readonly MethodInfo _createProxyForService = typeof(TestMiddlewareSource).GetTypeInfo().GetMethod(nameof(CreateProxyForService))!;
    }

    public class AutofacTestDependencyManager : AutofacDependencyManager
    {
        public override IDependencyManager Init()
        {
            base.Init();

            ContainerBuilder builder = GetContainerBuidler();

            builder.RegisterServiceMiddlewareSource(new TestMiddlewareSource());

            builder.ComponentRegistryBuilder.Registered += OnComponentRegistered;

            return this;
        }

        public void OnComponentRegistered(object sender, ComponentRegisteredEventArgs args)
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

        public virtual BlockingCollection<object> Objects { get; set; } = new BlockingCollection<object> { };

        public virtual string ReportObjects => string.Join(Environment.NewLine, Objects.Select(o => o.GetType().FullName).OrderBy(o => o));

        protected override void Dispose(bool disposing)
        {
            ContainerBuilder builder = GetContainerBuidler();

            builder.ComponentRegistryBuilder.Registered -= OnComponentRegistered;

            if (Objects != null)
            {
                foreach (object obj in Objects.Where(obj => Fake.IsFake(obj)))
                {
                    Fake.ClearConfiguration(obj);
                    Fake.ClearRecordedCalls(obj);
                }

                Objects.Dispose();
            }

            Objects = null;

            base.Dispose(disposing);
        }
    }
}
