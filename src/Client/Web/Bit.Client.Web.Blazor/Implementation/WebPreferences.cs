using System;
using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.JSInterop;

namespace Bit.Client.Web.Blazor.Implementation
{
    public class WebPreferences
    {
        private readonly IJSInProcessRuntime? _wasm_js_runtime;
        private static readonly ConcurrentDictionary<string, object> _keyValues = new ConcurrentDictionary<string, object>(); // to make server side blazor work! just for testing/debugging purposes!

        public WebPreferences(IJSRuntime jSRuntime)
        {
            _wasm_js_runtime = jSRuntime as IJSInProcessRuntime;
        }

        public virtual void Clear()
        {
            Clear(sharedName: "Shared");
        }

        public virtual void Clear(string sharedName)
        {
#if Android || iOS || UWP
            Microsoft.Maui.Storage.Preferences.Clear(sharedName);
#else
            if (_wasm_js_runtime != null)
                _wasm_js_runtime.InvokeVoid("localStorage.clear");
            else
                _keyValues.Clear();
#endif
        }

        public virtual bool ContainsKey(string key)
        {
            return ContainsKey(key, sharedName: "Shared");
        }

        public virtual bool ContainsKey(string key, string sharedName)
        {
            return Get(key, defaultValue: null, sharedName: sharedName) != null;
        }

        public virtual string Get(string key, string defaultValue)
        {
            return Get(key, defaultValue, sharedName: "Shared");
        }

        public virtual bool Get(string key, bool defaultValue)
        {
            return Get(key, defaultValue, sharedName: "Shared");
        }

        public virtual int Get(string key, int defaultValue)
        {
            return Get(key, defaultValue, sharedName: "Shared");
        }

        public virtual double Get(string key, double defaultValue)
        {
            return Get(key, defaultValue, sharedName: "Shared");
        }

        public virtual float Get(string key, float defaultValue)
        {
            return Get(key, defaultValue, sharedName: "Shared");
        }

        public virtual long Get(string key, long defaultValue)
        {
            return Get(key, defaultValue, sharedName: "Shared");
        }

        public virtual DateTime Get(string key, DateTime defaultValue)
        {
            return Get(key, defaultValue, sharedName: "Shared");
        }

        public virtual string Get(string key, string? defaultValue, string sharedName)
        {
            string? result = null;

#if Android || iOS || UWP
            result = Microsoft.Maui.Storage.Preferences.Get(key, defaultValue);
#else
            if (_wasm_js_runtime != null)
                result = _wasm_js_runtime.Invoke<string?>("localStorage.getItem", key);
            else
            {
                if (_keyValues.TryGetValue(key, out object value))
                    return (string)value;
                return defaultValue;
            }
#endif

            return result ?? defaultValue;
        }

        public virtual bool Get(string key, bool defaultValue, string sharedName)
        {
            string? result = Get(key, Convert.ToString(defaultValue, CultureInfo.InvariantCulture), sharedName: sharedName);

            return result != null ? Convert.ToBoolean(result, CultureInfo.InvariantCulture) : defaultValue;
        }

        public virtual int Get(string key, int defaultValue, string sharedName)
        {
            string? result = Get(key, Convert.ToString(defaultValue, CultureInfo.InvariantCulture), sharedName: sharedName);

            return result != null ? Convert.ToInt32(result, CultureInfo.InvariantCulture) : defaultValue;
        }

        public virtual double Get(string key, double defaultValue, string sharedName)
        {
            string? result = Get(key, Convert.ToString(defaultValue, CultureInfo.InvariantCulture), sharedName: sharedName);

            return result != null ? Convert.ToDouble(result, CultureInfo.InvariantCulture) : defaultValue;
        }

        public virtual float Get(string key, float defaultValue, string sharedName)
        {
            string? result = Get(key, Convert.ToString(defaultValue, CultureInfo.InvariantCulture), sharedName: sharedName);

            return result != null ? Convert.ToSingle(result, CultureInfo.InvariantCulture) : defaultValue;
        }

        public virtual long Get(string key, long defaultValue, string sharedName)
        {
            string? result = Get(key, Convert.ToString(defaultValue, CultureInfo.InvariantCulture), sharedName: sharedName);

            return result != null ? Convert.ToInt64(result, CultureInfo.InvariantCulture) : defaultValue;
        }

        public virtual DateTime Get(string key, DateTime defaultValue, string sharedName)
        {
            string? result = Get(key, Convert.ToString(defaultValue, CultureInfo.InvariantCulture), sharedName: sharedName);

            return result != null ? Convert.ToDateTime(result, CultureInfo.InvariantCulture) : defaultValue;
        }

        public virtual void Remove(string key)
        {
            Remove(key, sharedName: "Shared");
        }

        public virtual void Remove(string key, string sharedName)
        {
#if Android || iOS || UWP
            Microsoft.Maui.Storage.Preferences.Remove(key, sharedName);
#else
            if (_wasm_js_runtime != null)
                _wasm_js_runtime.InvokeVoid("localStorage.removeItem", key);
            else
                _keyValues.TryRemove(key, out object _);
#endif
        }

        public virtual void Set(string key, string value)
        {
            Set(key, value, sharedName: "Shared");
        }

        public virtual void Set(string key, bool value)
        {
            Set(key, value, sharedName: "Shared");
        }

        public virtual void Set(string key, int value)
        {
            Set(key, value, sharedName: "Shared");
        }

        public virtual void Set(string key, double value)
        {
            Set(key, value, sharedName: "Shared");
        }

        public virtual void Set(string key, float value)
        {
            Set(key, value, sharedName: "Shared");
        }

        public virtual void Set(string key, long value)
        {
            Set(key, value, sharedName: "Shared");
        }

        public virtual void Set(string key, DateTime value)
        {
            Set(key, value, sharedName: "Shared");
        }

        public virtual void Set(string key, string value, string sharedName)
        {
#if Android || iOS || UWP
            Microsoft.Maui.Storage.Preferences.Set(key, value, sharedName);
#else
            if (_wasm_js_runtime != null)
                _wasm_js_runtime.InvokeVoid("localStorage.setItem", key, value);
            else
                _keyValues.AddOrUpdate(key, value, (oldKey, oldValue) => value);
#endif
        }

        public virtual void Set(string key, bool value, string sharedName)
        {
            Set(key, Convert.ToString(value, CultureInfo.InvariantCulture), sharedName: sharedName);
        }

        public virtual void Set(string key, int value, string sharedName)
        {
            Set(key, Convert.ToString(value, CultureInfo.InvariantCulture), sharedName: sharedName);
        }

        public virtual void Set(string key, double value, string sharedName)
        {
            Set(key, Convert.ToString(value, CultureInfo.InvariantCulture), sharedName: sharedName);
        }

        public virtual void Set(string key, float value, string sharedName)
        {
            Set(key, Convert.ToString(value, CultureInfo.InvariantCulture), sharedName: sharedName);
        }

        public virtual void Set(string key, long value, string sharedName)
        {
            Set(key, Convert.ToString(value, CultureInfo.InvariantCulture), sharedName: sharedName);
        }

        public virtual void Set(string key, DateTime value, string sharedName)
        {
            Set(key, Convert.ToString(value, CultureInfo.InvariantCulture), sharedName: sharedName);
        }
    }
}
