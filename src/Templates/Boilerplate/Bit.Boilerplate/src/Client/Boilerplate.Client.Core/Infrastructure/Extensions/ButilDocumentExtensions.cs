namespace Bit.Butil;

/// <summary>
/// Temporary shim: DELETE this file (and index.ts's <c>setDocumentLang</c> bridge) once Bit.Butil ships
/// <c>Document.SetLang</c> - the real instance method then takes over at every call site.
/// </summary>
public static class ButilDocumentExtensions
{
    extension(Document document)
    {
        /// <summary>
        /// Sets the <c>lang</c> attribute of the document's root element.
        /// </summary>
        public async Task SetLang(string lang)
        {
            await GetJs(document).InvokeVoidAsync("setDocumentLang", lang);
        }
    }

    // Document takes its IJSRuntime as a primary-constructor parameter; '<js>P' is the field the compiler captures it into.
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "<js>P")]
    private static extern ref IJSRuntime GetJs(Document document);
}
