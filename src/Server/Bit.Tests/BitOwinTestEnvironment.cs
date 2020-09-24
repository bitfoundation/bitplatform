using Bit.Core;
using Bit.Core.Contracts;
using Bit.Test;
using IdentityModel.Client;
using Simple.OData.Client;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Bit.Tests
{
    public class BitOwinTestEnvironment : TestEnvironmentBase
    {
        static TestEnvironmentArgs ConfigureArgs(TestEnvironmentArgs args)
        {
            args ??= new TestEnvironmentArgs { };
            return args;
        }

        public BitOwinTestEnvironment(TestEnvironmentArgs args = null)
            : base(ConfigureArgs(args))
        {

        }

        protected override IAppModulesProvider GetAppModulesProvider(TestEnvironmentArgs args)
        {
            return args.CustomAppModulesProvider ?? new BitOwinCoreTestAppModulesProvider(args);
        }

        protected override IAppEnvironmentsProvider GetAppEnvironmentsProvider(TestEnvironmentArgs args)
        {
            return args.CustomAppEnvironmentsProvider ?? new BitTestAppEnvironmentsProvider(args);
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

        public IODataClient BuildBitODataClient(TokenResponse? token = null, ODataClientSettings? odataClientSettings = null)
        {
            odataClientSettings = odataClientSettings ?? new ODataClientSettings { };

            odataClientSettings.MetadataDocument = BitMetadata.MetadataString;

            return Server.BuildODataClient(token, odataRouteName: "Bit", odataClientSettings);
        }

        public IODataClient BuildTestODataClient(TokenResponse? token = null, ODataClientSettings? odataClientSettings = null)
        {
            odataClientSettings = odataClientSettings ?? new ODataClientSettings { };

            odataClientSettings.MetadataDocument = TestMetadata.MetadataString;

            return Server.BuildODataClient(token, odataRouteName: "Test", odataClientSettings);
        }
    }
}