using System.Security.Cryptography;
using System.Text;
using Bit.BlazeDb;
using Bit.BlazeDb.Storage;

namespace Bit.BlazeDb.Tests;

[TestClass]
public class EncryptionTests
{
    private static byte[] Key(byte seed = 1)
    {
        var key = new byte[32];
        Array.Fill(key, seed);
        return key;
    }

    private static BlazeDbEncryptedStorage Wrap(IBlazeDbStorage inner, byte seed = 1) =>
        new(inner, new BlazeDbAesGcmCipher(Key(seed)), ownsCipher: true);

    [TestMethod]
    public async Task Roundtrips_An_Atomic_Write()
    {
        var inner = new BlazeDbInMemoryStorage();
        using var storage = Wrap(inner);
        var payload = Encoding.UTF8.GetBytes("the quick brown fox");

        await storage.WriteAtomicAsync("snapshot", payload);

        Assert.AreSequenceEqual(payload, await storage.ReadAsync("snapshot"));
    }

    [TestMethod]
    public async Task Ciphertext_Does_Not_Contain_The_Plaintext()
    {
        var inner = new BlazeDbInMemoryStorage();
        using var storage = Wrap(inner);
        var payload = Encoding.UTF8.GetBytes("super-secret-value");

        await storage.WriteAtomicAsync("snapshot", payload);

        var stored = await inner.ReadAsync("snapshot");
        Assert.IsNotNull(stored);
        Assert.DoesNotContain("super-secret-value", Encoding.UTF8.GetString(stored!));
        Assert.IsTrue(stored!.Length > payload.Length);
    }

    [TestMethod]
    public async Task Appends_Concatenate_Back_Into_One_Stream()
    {
        var inner = new BlazeDbInMemoryStorage();
        using var storage = Wrap(inner);

        await storage.AppendAsync("wal", "alpha"u8.ToArray());
        await storage.AppendAsync("wal", "beta"u8.ToArray());
        await storage.AppendAsync("wal", "gamma"u8.ToArray());

        var read = await storage.ReadAsync("wal");
        Assert.AreEqual("alphabetagamma", Encoding.UTF8.GetString(read!));
    }

    [TestMethod]
    public async Task Each_Write_Uses_A_Fresh_Nonce()
    {
        var inner = new BlazeDbInMemoryStorage();
        using var storage = Wrap(inner);

        await storage.AppendAsync("wal", "same"u8.ToArray());
        await storage.AppendAsync("wal", "same"u8.ToArray());

        var stored = (await inner.ReadAsync("wal"))!;
        var half = stored.Length / 2;
        Assert.IsFalse(Enumerable.SequenceEqual(stored[..half], stored[half..]));
    }

    [TestMethod]
    public async Task Missing_File_Still_Reads_As_Null()
    {
        using var storage = Wrap(new BlazeDbInMemoryStorage());
        Assert.IsNull(await storage.ReadAsync("nope"));
    }

    [TestMethod]
    public async Task Quota_Awareness_Passes_Through_The_Wrapper()
    {
        // Encrypting a browser database must not cost it the engine's quota handling.
        using var quotaAware = Wrap(new QuotaLimitedStorage { QuotaBytes = 1234 });
        Assert.AreEqual(1234, (await quotaAware.GetQuotaAsync())!.Value.QuotaBytes);

        using var plain = Wrap(new BlazeDbInMemoryStorage());
        Assert.IsNull(await plain.GetQuotaAsync());
    }

    [TestMethod]
    public async Task Wrong_Key_Is_Reported_As_Corruption()
    {
        var inner = new BlazeDbInMemoryStorage();
        using (var writer = Wrap(inner, seed: 1))
        {
            await writer.WriteAtomicAsync("snapshot", "payload"u8.ToArray());
        }

        using var reader = Wrap(inner, seed: 2);
        await Assert.ThrowsExactlyAsync<BlazeDbCorruptDatabaseException>(async () => await reader.ReadAsync("snapshot"));
    }

    [TestMethod]
    public async Task Tampered_Ciphertext_Is_Reported_As_Corruption()
    {
        var inner = new BlazeDbInMemoryStorage();
        using var storage = Wrap(inner);
        await storage.WriteAtomicAsync("snapshot", "payload"u8.ToArray());

        var stored = (await inner.ReadAsync("snapshot"))!;
        stored[^1] ^= 0xFF;
        await inner.WriteAtomicAsync("snapshot", stored);

        await Assert.ThrowsExactlyAsync<BlazeDbCorruptDatabaseException>(async () => await storage.ReadAsync("snapshot"));
    }

    [TestMethod]
    public async Task A_Frame_Cannot_Be_Replayed_Into_Another_File()
    {
        var inner = new BlazeDbInMemoryStorage();
        using var storage = Wrap(inner);
        await storage.WriteAtomicAsync("wal", "payload"u8.ToArray());

        // The file name is bound in as associated data, so moving the bytes breaks the seal.
        await inner.WriteAtomicAsync("snapshot", (await inner.ReadAsync("wal"))!);

        await Assert.ThrowsExactlyAsync<BlazeDbCorruptDatabaseException>(async () => await storage.ReadAsync("snapshot"));
    }

    [TestMethod]
    public async Task A_Damaged_Frame_Length_Is_Corruption_Not_A_Torn_Tail()
    {
        // The length sits outside the sealed body. Unchecked, a length damaged into something past
        // the end of the file would look exactly like a crash mid-frame, and the frames behind it
        // would be dropped without a word.
        var inner = new BlazeDbInMemoryStorage();
        using var storage = Wrap(inner);
        await storage.AppendAsync("wal", "first"u8.ToArray());
        await storage.AppendAsync("wal", "second"u8.ToArray());

        // Frame layout: version(1) nonce(12) length(4) header crc(4) body. Inflate the first length.
        var stored = (await inner.ReadAsync("wal"))!;
        stored[1 + 12 + 2] = 0x7F;
        await inner.WriteAtomicAsync("wal", stored);

        await Assert.ThrowsExactlyAsync<BlazeDbCorruptDatabaseException>(async () => await storage.ReadAsync("wal"));
    }

    [TestMethod]
    public async Task A_Torn_Trailing_Frame_Is_Ignored()
    {
        var inner = new BlazeDbInMemoryStorage();
        using var storage = Wrap(inner);
        await storage.AppendAsync("wal", "committed"u8.ToArray());
        var complete = (await inner.ReadAsync("wal"))!;
        await storage.AppendAsync("wal", "interrupted"u8.ToArray());

        // Simulate a crash midway through the second append.
        var torn = (await inner.ReadAsync("wal"))!;
        await inner.WriteAtomicAsync("wal", torn[..(complete.Length + 9)]);

        Assert.AreEqual("committed", Encoding.UTF8.GetString((await storage.ReadAsync("wal"))!));
    }

    [TestMethod]
    public async Task An_Append_After_A_Torn_Frame_Cuts_The_Fragment_Off()
    {
        // Ignoring the fragment on the way out is not enough: it is still in the file, and an
        // append landing behind it would be swallowed along with it on every later read.
        var inner = new BlazeDbInMemoryStorage();
        using var storage = Wrap(inner);
        await storage.AppendAsync("wal", "committed"u8.ToArray());
        var complete = (await inner.ReadAsync("wal"))!;
        await storage.AppendAsync("wal", "interrupted"u8.ToArray());
        var torn = (await inner.ReadAsync("wal"))!;
        await inner.WriteAtomicAsync("wal", torn[..(complete.Length + 9)]);

        Assert.AreEqual("committed", Encoding.UTF8.GetString((await storage.ReadAsync("wal"))!));

        await storage.AppendAsync("wal", "later"u8.ToArray());

        Assert.AreEqual("committedlater", Encoding.UTF8.GetString((await storage.ReadAsync("wal"))!));
    }

    [TestMethod]
    public async Task Commits_After_A_Torn_Encrypted_Wal_Append_Still_Persist()
    {
        var inner = new BlazeDbInMemoryStorage();
        var options = () => new BlazeDbDatabaseOptions
        {
            Storage = Wrap(inner),
            FlushInterval = TimeSpan.FromHours(1),
        }.AddTable(PersonTable.Descriptor);

        var db = await BlazeDbDatabase.OpenAsync(options());
        db.GetTable(PersonTable.Descriptor).Insert(new Person(1, "one", 1));
        await db.FlushAsync();
        var whole = (await inner.ReadAsync("wal-1.blz"))!;

        db.GetTable(PersonTable.Descriptor).Insert(new Person(2, "two", 2));
        await db.FlushAsync();
        var torn = (await inner.ReadAsync("wal-1.blz"))!;
        await db.DisposeAsync();

        // Crash midway through the frame the second flush was writing.
        await inner.WriteAtomicAsync("wal-1.blz", torn[..(whole.Length + 9)]);

        var reopened = await BlazeDbDatabase.OpenAsync(options());
        Assert.AreEqual(1, reopened.GetTable(PersonTable.Descriptor).Count);

        reopened.GetTable(PersonTable.Descriptor).Insert(new Person(3, "three", 3));
        await reopened.FlushAsync();
        await reopened.DisposeAsync();

        var third = await BlazeDbDatabase.OpenAsync(options());
        await using var owned = third;
        Assert.AreEqual(2, third.GetTable(PersonTable.Descriptor).Count);
        Assert.AreEqual("three", third.GetTable(PersonTable.Descriptor).Get(3)!.Name);
    }

    [TestMethod]
    public async Task A_Database_Round_Trips_Through_Encrypted_Storage()
    {
        var inner = new BlazeDbInMemoryStorage();
        var id = Guid.NewGuid();

        using (var storage = Wrap(inner))
        {
            var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions
            {
                Storage = storage,
                FlushInterval = TimeSpan.FromHours(1),
            }.AddTable(TodoItem.Table));
            db.GetTable(TodoItem.Table).Insert(new TodoItem { Id = id, Title = "encrypted at rest" });
            await db.FlushAsync();
            await db.DisposeAsync();
        }

        // Nothing readable is left behind on the underlying storage.
        var walBytes = await inner.ReadAsync("wal-1.blz");
        Assert.IsNotNull(walBytes);
        Assert.DoesNotContain("encrypted at rest", Encoding.UTF8.GetString(walBytes!));

        using (var storage = Wrap(inner))
        {
            var reopened = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions
            {
                Storage = storage,
                FlushInterval = TimeSpan.FromHours(1),
            }.AddTable(TodoItem.Table));
            await using var owned = reopened;

            Assert.AreEqual("encrypted at rest", reopened.GetTable(TodoItem.Table).Get(id)!.Title);
        }
    }

    [TestMethod]
    public async Task Encrypted_Database_Survives_A_Checkpoint()
    {
        var inner = new BlazeDbInMemoryStorage();
        using var storage = Wrap(inner);
        var options = new BlazeDbDatabaseOptions
        {
            Storage = storage,
            FlushInterval = TimeSpan.FromHours(1),
        }.AddTable(TodoItem.Table);

        var db = await BlazeDbDatabase.OpenAsync(options);
        var todos = db.GetTable(TodoItem.Table);
        for (var i = 0; i < 50; i++)
        {
            todos.Insert(new TodoItem { Id = Guid.NewGuid(), Title = "row " + i });
        }
        await db.CheckpointAsync();
        await db.DisposeAsync();

        var reopened = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions
        {
            Storage = storage,
            FlushInterval = TimeSpan.FromHours(1),
        }.AddTable(TodoItem.Table));
        await using var owned = reopened;

        Assert.AreEqual(50, reopened.GetTable(TodoItem.Table).Count);
    }

    [TestMethod]
    public void Derived_Keys_Are_Stable_And_Salt_Dependent()
    {
        var salt = "0123456789abcdef"u8.ToArray();
        var other = "fedcba9876543210"u8.ToArray();

        var a = BlazeDbEncryptedStorage.DeriveKey("correct horse battery staple", salt, iterations: 1000);
        var b = BlazeDbEncryptedStorage.DeriveKey("correct horse battery staple", salt, iterations: 1000);
        var c = BlazeDbEncryptedStorage.DeriveKey("correct horse battery staple", other, iterations: 1000);

        Assert.AreSequenceEqual(a, b);
        Assert.IsFalse(a.SequenceEqual(c));
        Assert.AreEqual(32, a.Length);
    }

    [TestMethod]
    public void Derive_Key_Rejects_Weak_Inputs()
    {
        Assert.ThrowsExactly<ArgumentException>(() => BlazeDbEncryptedStorage.DeriveKey("", "0123456789abcdef"u8));
        Assert.ThrowsExactly<ArgumentException>(() => BlazeDbEncryptedStorage.DeriveKey("passphrase", "short"u8));
    }

    [TestMethod]
    public async Task Passphrase_Derived_Key_Decrypts_What_It_Encrypted()
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var inner = new BlazeDbInMemoryStorage();
        var key = BlazeDbEncryptedStorage.DeriveKey("hunter2-but-longer", salt, iterations: 1000);

        using (var storage = new BlazeDbEncryptedStorage(inner, new BlazeDbAesGcmCipher(key), ownsCipher: true))
        {
            await storage.WriteAtomicAsync("snapshot", "payload"u8.ToArray());
        }

        var sameKey = BlazeDbEncryptedStorage.DeriveKey("hunter2-but-longer", salt, iterations: 1000);
        using var reader = new BlazeDbEncryptedStorage(inner, new BlazeDbAesGcmCipher(sameKey), ownsCipher: true);
        Assert.AreSequenceEqual("payload"u8.ToArray(), await reader.ReadAsync("snapshot"));
    }
}
