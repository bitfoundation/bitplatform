using Bit.Core;
using Foundation.Test;
using IdentityServer.Api.Contracts;
using IdentityServer.Test.Api.Implementations;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace IdentityServer.Test
{
    public class IdentityServerTestEnvironment : TestEnvironmentBase
    {
        public IdentityServerTestEnvironment(TestEnvironmentArgs args)
            : base(args)
        {

        }

        public IdentityServerTestEnvironment(bool useProxyBasedDependencyManager = true, bool useRealServer = true) :
            this(new TestEnvironmentArgs
            {
                UseHttps = false,
                UseRealServer = useRealServer,
                UseProxyBasedDependencyManager = useProxyBasedDependencyManager,
                Port = 8080,
                CustomDependenciesManagerProvider = new IdentityServerTestDependenciesManagerProvider(),
                CustomAppEnvironmentProvider = new IdentityServerTestAppEnvironmentProvider()
            })
        {

        }

        protected override List<Func<TypeInfo, bool>> GetAutoProxyCreationIncludeRules()
        {
            List<Func<TypeInfo, bool>> baseIncludeRulesList = base.GetAutoProxyCreationIncludeRules();

            baseIncludeRulesList.AddRange(new List<Func<TypeInfo, bool>>
            {
                serviceType => serviceType.Assembly == AssemblyContainer.Current.GetBitIdentityServerAssembly(),
                serviceType => serviceType.Assembly == AssemblyContainer.Current.GetBitIdentityServerTestsAssembly()}
            );

            return baseIncludeRulesList;
        }
    }
}
