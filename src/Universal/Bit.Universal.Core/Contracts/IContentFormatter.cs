using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Core.Contracts
{
    /// <summary>
    /// It serialize/deSerialize objects to/from json/xml etc based on what implementation is provided.
    /// </summary>
    public interface IContentFormatter
    {
        string Serialize<T>([AllowNull] T obj);

        T Deserialize<T>(string objAsStr);

        Task<T> DeserializeAsync<T>(Stream input, CancellationToken cancellationToken);

        string ContentType { get; }
    }
}