using System;
using System.Collections.Generic;
using System.Reflection;
using Bit.Core;
using Bit.Core.Contracts;
using Bit.Test;

namespace Bit.Tests
{
    public class BitOwinTestEnvironment : TestEnvironmentBase
    {
        public BitOwinTestEnvironment(TestEnvironmentArgs args = null)
            : base(args)
        {

        }

        protected override IAppModulesProvider GetAppModulesProvider(TestEnvironmentArgs args)
        {
            return args.CustomAppModulesProvider ?? (args.UseAspNetCore ? new BitOwinCoreTestAppModulesProvider(args) : (IAppModulesProvider)new BitOwinTestAppModulesProvider(args));
        }

        protected override IAppEnvironmentProvider GetAppEnvironmentProvider(TestEnvironmentArgs args)
        {
            return args.CustomAppEnvironmentProvider ?? new BitTestAppEnvironmentProvider(args);
        }

        protected override List<Func<TypeInfo, bool>> GetAutoProxyCreationIncludeRules()
        {
            List<Func<TypeInfo, bool>> baseList = base.GetAutoProxyCreationIncludeRules();

            baseList.AddRange(new List<Func<TypeInfo, bool>>
            {
                implementationType => implementationType.Assembly == AssemblyContainer.Current.GetBitTestsAssembly()
            });

            return baseList;
        }
    }
}