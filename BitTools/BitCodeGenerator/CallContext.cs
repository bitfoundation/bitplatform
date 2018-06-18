using System.Collections.Concurrent;
using System.Threading;

// http://www.cazzulino.com/callcontext-netstandard-netcore.html

namespace System.Runtime.Remoting.Messaging
{
    public static class CallContext
    {
        private static readonly ConcurrentDictionary<string, AsyncLocal<object>> state = new ConcurrentDictionary<string, AsyncLocal<object>>();

        /// <summary>
        /// Retrieves an object with the specified name from the <see cref="CallContext"/>.
        /// </summary>
        /// <param name="name">The name of the item in the call context.</param>
        /// <returns>The object in the call context associated with the specified name, or <see langword="null"/> if not found.</returns>
        public static object LogicalGetData(string name) =>
            state.TryGetValue(name, out AsyncLocal<object> data) ? data.Value : null;
    }
}
