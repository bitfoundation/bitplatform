namespace Bit.BevTools;

/// <summary>How the requests of the <c>HttpClient</c> registered in DI reach an attached session.</summary>
internal enum BevToolsHttpClientAttachment
{
    /// <summary>No <c>HttpClient</c> is registered in DI, or its handler could not be reached.</summary>
    None,

    /// <summary>
    /// Its pipeline already holds a handler that records: Bit.BevTools.Loader's <c>BevToolsHttpHookHandler</c>, or the
    /// hook the loader adds to every <c>IHttpClientFactory</c> pipeline when the client comes from the factory.
    /// </summary>
    InPipeline,

    /// <summary>BevTools put a <see cref="BevToolsHttpMessageHandler"/> in front of its handler through reflection.</summary>
    Wrapped,

    /// <summary>It is transient: wrapping one instance would leave every other one unrecorded, so none is.</summary>
    Transient,
}
