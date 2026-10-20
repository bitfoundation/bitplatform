using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Bit.BlazeDb.Wal;

namespace Bit.BlazeDb.Storage;

/// <summary>
/// Wraps any <see cref="IBlazeDbStorage"/> so everything the engine persists - WAL records, snapshots and
/// the manifest - is encrypted before it reaches the disk or the origin's file system.
/// <para>
/// Each write becomes a self-contained frame of
/// <c>[version][nonce][length][header CRC][ciphertext+tag]</c>. Framing per write is what keeps
/// <see cref="AppendAsync"/> a genuine append: the WAL can keep growing without rewriting or
/// re-encrypting what came before, and a read simply decrypts the frames in order and concatenates
/// them back into the original byte stream. The file name is mixed in as associated data, so a frame
/// lifted from one file cannot be replayed into another.
/// </para>
/// <para>
/// The length is outside the sealed body - it has to be, since it says where that body ends - so it
/// carries its own checksum. Without one, a corrupted length that happened to point past the end of
/// the file would be indistinguishable from a crash mid-frame, and the frames after it would be
/// dropped silently instead of reported as the corruption they are.
/// </para>
/// <para>
/// A short trailing frame is treated as a torn write and ignored, matching the WAL's own tolerance
/// for a half-written tail. A frame that is complete but fails authentication is reported as
/// corruption rather than silently skipped. Ignoring a torn tail on the way out is only half the
/// job: the bytes are still in the file, so the offset it starts at is remembered and the next
/// append trims the file back to whole frames first - otherwise that append would land behind the
/// fragment, which would swallow it (and everything after it) on every later read.
/// </para>
/// </summary>
public sealed class BlazeDbEncryptedStorage : IBlazeDbQuotaAwareStorage, IBlazeDbEnumerableStorage, IDisposable
{
    private const byte FormatVersion = 1;
    private const int LengthPrefixSize = sizeof(int);
    private const int HeaderCrcSize = sizeof(uint);

    private readonly IBlazeDbStorage _inner;
    private readonly IBlazeDbAeadCipher _cipher;
    private readonly bool _ownsCipher;

    // Files whose last read stopped at a torn frame, and the offset that frame starts at.
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, long> _tornTails = new();

    /// <param name="inner">The storage that ends up holding the ciphertext.</param>
    /// <param name="cipher">The cipher to seal each frame with.</param>
    /// <param name="ownsCipher">When true, disposing this also disposes <paramref name="cipher"/>.</param>
    public BlazeDbEncryptedStorage(IBlazeDbStorage inner, IBlazeDbAeadCipher cipher, bool ownsCipher = false)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _cipher = cipher ?? throw new ArgumentNullException(nameof(cipher));
        _ownsCipher = ownsCipher;
    }

    private int HeaderSize => 1 + _cipher.NonceSizeInBytes + LengthPrefixSize + HeaderCrcSize;

    /// <summary>
    /// Derives a key from a passphrase with PBKDF2-HMAC-SHA256, which is one of the few
    /// cryptographic primitives available both on desktop and inside the browser sandbox.
    /// The salt must be stored alongside the database and must not be reused across databases.
    /// </summary>
    public static byte[] DeriveKey(string passphrase, ReadOnlySpan<byte> salt, int iterations = 210_000, int keySizeInBytes = 32)
    {
        if (string.IsNullOrEmpty(passphrase))
        {
            throw new ArgumentException("Passphrase must be non-empty.", nameof(passphrase));
        }
        if (salt.Length < 8)
        {
            throw new ArgumentException("Salt must be at least 8 bytes.", nameof(salt));
        }
        return Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(passphrase), salt, iterations, HashAlgorithmName.SHA256, keySizeInBytes);
    }

    public async ValueTask<byte[]?> ReadAsync(string name, CancellationToken cancellationToken = default)
    {
        var raw = await _inner.ReadAsync(name, cancellationToken).ConfigureAwait(false);
        // Whatever this read finds is the current truth about the file, so anything a previous one
        // noted about it is stale.
        _tornTails.TryRemove(name, out _);
        if (raw is null)
        {
            return null;
        }
        if (raw.Length == 0)
        {
            return [];
        }

        var associatedData = Encoding.UTF8.GetBytes(name);
        var plaintext = new List<byte[]>();
        var total = 0;
        var offset = 0;

        while (offset < raw.Length)
        {
            if (raw.Length - offset < HeaderSize)
            {
                RememberTornTail(name, offset);
                break; // Torn header.
            }
            // The version, nonce and length are checksummed together, so a frame either describes
            // itself exactly as it was written or is reported as corrupt. Only then can a body that
            // runs past the end of the file be trusted to mean a crash mid-write rather than a
            // damaged length - the two are the same bytes otherwise, and treating corruption as a
            // torn tail would quietly drop every frame behind it.
            var headerAt = offset;
            var describedSize = HeaderSize - HeaderCrcSize;
            var expectedCrc = BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(headerAt + describedSize, HeaderCrcSize));
            if (BlazeDbCrc32.Compute(raw.AsSpan(headerAt, describedSize)) != expectedCrc)
            {
                // A header the tail of a crashed write left half-finished is the one case where a
                // mismatch is expected rather than damage: nothing follows it to lose.
                if (raw.Length - offset <= HeaderSize)
                {
                    RememberTornTail(name, offset);
                    break;
                }
                throw new BlazeDbCorruptDatabaseException(
                    $"An encryption frame header in '{name}' failed checksum validation; the file is damaged.");
            }

            if (raw[offset] != FormatVersion)
            {
                throw new BlazeDbCorruptDatabaseException(
                    $"Unrecognized encryption frame version {raw[offset]} in '{name}'.");
            }

            var nonce = raw.AsMemory(offset + 1, _cipher.NonceSizeInBytes);
            var lengthAt = offset + 1 + _cipher.NonceSizeInBytes;
            var length = BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(lengthAt, LengthPrefixSize));
            if (length < _cipher.TagSizeInBytes)
            {
                throw new BlazeDbCorruptDatabaseException($"Invalid encryption frame length {length} in '{name}'.");
            }

            var bodyAt = headerAt + HeaderSize;
            if (raw.Length - bodyAt < length)
            {
                RememberTornTail(name, offset);
                break; // Torn body.
            }

            byte[] chunk;
            try
            {
                chunk = await _cipher
                    .DecryptAsync(nonce, raw.AsMemory(bodyAt, length), associatedData, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is CryptographicException or ArgumentException)
            {
                throw new BlazeDbCorruptDatabaseException(
                    $"Failed to authenticate encrypted data in '{name}'. The file was tampered with, " +
                    "truncated mid-frame, or the key is wrong.", ex);
            }

            plaintext.Add(chunk);
            total += chunk.Length;
            offset = bodyAt + length;
        }

        if (plaintext.Count == 1)
        {
            return plaintext[0];
        }

        var result = new byte[total];
        var written = 0;
        foreach (var chunk in plaintext)
        {
            chunk.CopyTo(result, written);
            written += chunk.Length;
        }
        return result;
    }

    public async ValueTask WriteAtomicAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        var frame = await SealAsync(name, data, cancellationToken).ConfigureAwait(false);
        await _inner.WriteAtomicAsync(name, frame, cancellationToken).ConfigureAwait(false);
        // The file is now exactly this one frame, so any fragment that was in it is gone.
        _tornTails.TryRemove(name, out _);
    }

    public async ValueTask AppendAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        if (data.IsEmpty)
        {
            return;
        }
        var frame = await SealAsync(name, data, cancellationToken).ConfigureAwait(false);
        await TrimTornTailAsync(name, cancellationToken).ConfigureAwait(false);
        await _inner.AppendAsync(name, frame, cancellationToken).ConfigureAwait(false);
    }

    public ValueTask DeleteAsync(string name, CancellationToken cancellationToken = default)
    {
        _tornTails.TryRemove(name, out _);
        return _inner.DeleteAsync(name, cancellationToken);
    }

    /// <summary>
    /// Notes where a frame the last read could not finish begins, so the next append can cut it
    /// off. Zero-length would mean the file holds nothing whole, which a rewrite still handles.
    /// </summary>
    private void RememberTornTail(string name, long offset) => _tornTails[name] = offset;

    /// <summary>
    /// Rewrites a file back to the frames that were whole, discarding a fragment a crash left at
    /// the end. Done here, immediately before an append, rather than during the read that found it:
    /// a read must stay a read, not least because a read-only replica shares this code and has no
    /// business repairing the writer's files.
    /// </summary>
    private async ValueTask TrimTornTailAsync(string name, CancellationToken cancellationToken)
    {
        if (!_tornTails.TryGetValue(name, out var validLength))
        {
            return;
        }
        var raw = await _inner.ReadAsync(name, cancellationToken).ConfigureAwait(false);
        if (raw is not null && raw.Length > validLength)
        {
            await _inner
                .WriteAtomicAsync(name, raw.AsMemory(0, (int)validLength), cancellationToken)
                .ConfigureAwait(false);
        }
        // Cleared only now: a failed rewrite has to be retried, or the append it was making room
        // for would land behind the fragment after all.
        _tornTails.TryRemove(name, out _);
    }

    /// <summary>
    /// Passes the wrapped backend's estimate through, so encrypting a browser database does not
    /// cost it the engine's quota handling. Null when the backend cannot report usage.
    /// </summary>
    public ValueTask<BlazeDbStorageQuota?> GetQuotaAsync(CancellationToken cancellationToken = default) =>
        _inner is IBlazeDbQuotaAwareStorage quotaAware ? quotaAware.GetQuotaAsync(cancellationToken) : default;

    /// <summary>
    /// Whether the wrapped backend rewrites a file to append to it. Encryption does not change that,
    /// and the engine has to know: a browser backend behind this wrapper still needs room for the
    /// whole log before it can extend it.
    /// </summary>
    public bool AppendRewritesWholeFile =>
        _inner is IBlazeDbQuotaAwareStorage { AppendRewritesWholeFile: true };

    /// <summary>
    /// The wrapped backend's file names, which are not encrypted - only the contents are - so a
    /// database behind this wrapper can still be removed completely.
    /// </summary>
    public ValueTask<IReadOnlyCollection<string>> ListAsync(CancellationToken cancellationToken = default) =>
        _inner is IBlazeDbEnumerableStorage enumerable
            ? enumerable.ListAsync(cancellationToken)
            : throw new NotSupportedException(
                "The storage behind this BlazeDbEncryptedStorage cannot list its files.");

    private async ValueTask<byte[]> SealAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        var nonce = new byte[_cipher.NonceSizeInBytes];
        RandomNumberGenerator.Fill(nonce);

        var associatedData = Encoding.UTF8.GetBytes(name);
        var ciphertext = await _cipher
            .EncryptAsync(nonce, data, associatedData, cancellationToken)
            .ConfigureAwait(false);

        var frame = new byte[HeaderSize + ciphertext.Length];
        frame[0] = FormatVersion;
        nonce.CopyTo(frame, 1);
        BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(1 + nonce.Length, LengthPrefixSize), ciphertext.Length);
        var describedSize = HeaderSize - HeaderCrcSize;
        BinaryPrimitives.WriteUInt32LittleEndian(
            frame.AsSpan(describedSize, HeaderCrcSize), BlazeDbCrc32.Compute(frame.AsSpan(0, describedSize)));
        ciphertext.CopyTo(frame, HeaderSize);
        return frame;
    }

    public void Dispose()
    {
        if (_ownsCipher && _cipher is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
