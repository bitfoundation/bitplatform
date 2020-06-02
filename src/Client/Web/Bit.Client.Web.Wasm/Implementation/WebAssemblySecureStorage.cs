using System.Threading.Tasks;
using Xamarin.Essentials.Interfaces;

namespace Bit.Client.Web.Wasm.Implementation
{
    public class WebAssemblySecureStorage : ISecureStorage
    {
        private readonly IPreferences _preferences;

        public WebAssemblySecureStorage(IPreferences preferences)
        {
            _preferences = preferences;
        }

        public virtual Task<string> GetAsync(string key)
        {
            return Task.FromResult(_preferences.Get(key, null));
        }

        public virtual bool Remove(string key)
        {
            var exists = _preferences.ContainsKey(key);

            _preferences.Remove(key);

            return exists;
        }

        public virtual void RemoveAll()
        {
            _preferences.Clear();
        }

        public virtual Task SetAsync(string key, string value)
        {
            _preferences.Set(key, value);

            return Task.CompletedTask;
        }
    }
}
