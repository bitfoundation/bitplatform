namespace FluentStorage.Storage;

public static class IStoreExtensions
{
    extension(IStore store)
    {
        /// <summary>
        /// Deletes a single object. Not every provider implements <see cref="IStore.DeleteObject"/> - Azure Blob Storage
        /// overrides only the plural, so the singular reaches <c>StoreBase</c> and throws <see cref="NotSupportedException"/>.
        /// </summary>
        public async Task DeleteSingleObject(string path, CancellationToken cancellationToken)
        {
            await store.DeleteObjects([path], cancellationToken);
        }
    }
}
