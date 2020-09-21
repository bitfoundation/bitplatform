using System;
using System.Threading.Tasks;
using Xamarin.Essentials.Interfaces;

namespace Bit.Client.Web.Wasm.Implementation
{
    public class WebAssemblyClipboard : IClipboard
    {
        public bool HasText => false;

        public event EventHandler<EventArgs> ClipboardContentChanged = default!;

        public Task<string> GetTextAsync()
        {
            return Task.FromResult(string.Empty);
        }

        public Task SetTextAsync(string text)
        {
            return Task.CompletedTask;
        }
    }
}
