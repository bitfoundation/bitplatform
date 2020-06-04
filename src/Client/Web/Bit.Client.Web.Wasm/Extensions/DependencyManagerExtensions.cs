using Bit.Client.Web.Wasm.Implementation;
using System;
using Xamarin.Essentials.Interfaces;

namespace Bit.Core.Contracts
{
    public static class DependencyManagerExtensions
    {
        public static IDependencyManager RegisterXamarinEssentials(this IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<IAppInfo, WebAssemblyAppInfo>(lifeCycle: DependencyLifeCycle.Transient, overwriteExisting: false); dependencyManager.Register<IAppInfo, WebAssemblyAppInfo>(lifeCycle: DependencyLifeCycle.Transient, overwriteExisting: false);
            dependencyManager.Register<IClipboard, WebAssemblyClipboard>(lifeCycle: DependencyLifeCycle.Transient, overwriteExisting: false);
            dependencyManager.Register<IConnectivity, WebAssemblyConnectivity>(lifeCycle: DependencyLifeCycle.Transient, overwriteExisting: false);
            dependencyManager.Register<IDeviceInfo, WebAssemblyDeviceInfo>(lifeCycle: DependencyLifeCycle.Transient, overwriteExisting: false);
            dependencyManager.Register<IMainThread, WebAssemblyMainThread>(lifeCycle: DependencyLifeCycle.Transient, overwriteExisting: false);
            dependencyManager.Register<IPreferences, WebAssemblyPreferences>(lifeCycle: DependencyLifeCycle.Transient, overwriteExisting: false);
            dependencyManager.Register<ISecureStorage, WebAssemblySecureStorage>(lifeCycle: DependencyLifeCycle.Transient, overwriteExisting: false);
            dependencyManager.Register<IVersionTracking, WebAssemblyVersionTracking>(lifeCycle: DependencyLifeCycle.Transient, overwriteExisting: false);
            dependencyManager.Register<IWebAuthenticator, WebAssemblyWebAuthenticator>(lifeCycle: DependencyLifeCycle.Transient, overwriteExisting: false);

            return dependencyManager;
        }
    }
}
