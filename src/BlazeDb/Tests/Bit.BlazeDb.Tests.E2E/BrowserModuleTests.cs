using Bit.BlazeDb.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.BlazeDb.Tests.E2E;

/// <summary>The ES modules Bit.BlazeDb.Browser ships under _content/Bit.BlazeDb.Browser/.</summary>
internal static class BrowserModules
{
    public static readonly string[] All = ["bit-blazedb-opfs", "bit-blazedb-idb", "bit-blazedb-crypto", "bit-blazedb-tabs"];
}

/// <summary>
/// The browser half of the storage backends, driven in Chromium through the very modules the package
/// ships - the [JSImport] bindings call exactly these exports. The demo puts the OPFS backend through a
/// real database (see <see cref="PersistenceTests"/>); these cover what no demo page reaches on its own:
/// IndexedDB's read-modify-write appends and its no-create open, OPFS appends and the Web Lock election,
/// AES-GCM through WebCrypto, and the BroadcastChannel fan-out between tabs.
/// </summary>
[TestClass]
public class BrowserModuleTests : BlazeDbTest
{
    [TestMethod]
    public async Task IndexedDb_writes_appends_reads_and_deletes_files()
    {
        var page = await OpenAsync("/");

        var result = await page.EvaluateAsync<string[]>(
            """
            async () => {
              const idb = await import('./_content/Bit.BlazeDb.Browser/bit-blazedb-idb.js');
              const name = 'e2e-' + crypto.randomUUID();
              const text = bytes => bytes === null ? 'null' : new TextDecoder().decode(bytes);

              const missing = await idb.openExistingDatabase(name);
              const db = await idb.openDatabase(name);
              await idb.writeAtomic(db, 'manifest', new TextEncoder().encode('first'));
              await idb.appendFile(db, 'wal', new TextEncoder().encode('ab'));
              await idb.appendFile(db, 'wal', new TextEncoder().encode('cd'));
              const manifest = text(await idb.readFile(db, 'manifest'));
              const wal = text(await idb.readFile(db, 'wal'));
              await idb.deleteFile(db, 'wal');
              const deleted = text(await idb.readFile(db, 'wal'));
              idb.closeDatabase(db);
              const existing = await idb.openExistingDatabase(name);
              const reopened = text(await idb.readFile(existing, 'manifest'));
              idb.closeDatabase(existing);

              return [String(missing), manifest, wal, deleted, reopened, String(await idb.isOpfsAvailable())];
            }
            """);

        CollectionAssert.AreEqual(new[] { "null", "first", "abcd", "null", "first", "true" }, result);
    }

    [TestMethod]
    public async Task Opfs_writes_appends_reads_and_deletes_files()
    {
        var page = await OpenAsync("/");

        var result = await page.EvaluateAsync<string[]>(
            """
            async () => {
              const opfs = await import('./_content/Bit.BlazeDb.Browser/bit-blazedb-opfs.js');
              const name = 'e2e-' + crypto.randomUUID();
              const text = bytes => bytes === null ? 'null' : new TextDecoder().decode(bytes);

              const missing = await opfs.openExistingDatabaseDirectory(name);
              const dir = await opfs.openDatabaseDirectory(name);
              await opfs.writeAtomic(dir, 'manifest', new TextEncoder().encode('first'));
              await opfs.writeAtomic(dir, 'manifest', new TextEncoder().encode('second'));
              await opfs.appendFile(dir, 'wal', new TextEncoder().encode('ab'));
              await opfs.appendFile(dir, 'wal', new TextEncoder().encode('cd'));
              const manifest = text(await opfs.readFile(dir, 'manifest'));
              const wal = text(await opfs.readFile(dir, 'wal'));
              await opfs.deleteFile(dir, 'wal');
              await opfs.deleteFile(dir, 'never-written');
              const deleted = text(await opfs.readFile(dir, 'wal'));
              const estimate = await opfs.storageEstimate();

              return [String(missing), manifest, wal, deleted, String(opfs.canWrite()), String(estimate !== null && estimate.quota > 0)];
            }
            """);

        CollectionAssert.AreEqual(new[] { "null", "second", "abcd", "null", "true", "true" }, result);
    }

    [TestMethod]
    public async Task The_writer_lock_is_won_once_and_released_on_demand()
    {
        var context = await NewContextAsync();

        var first = await context.NewPageAsync();
        await GotoAsync(first, "/mcp-server");
        var second = await context.NewPageAsync();
        await GotoAsync(second, "/mcp-server");

        const string Import = "await import('./_content/Bit.BlazeDb.Browser/bit-blazedb-opfs.js')";
        var lockName = $"e2e-lock-{Guid.NewGuid():N}";

        Assert.IsTrue(await first.EvaluateAsync<bool>($"async name => ({Import}).acquireLock(name, false)", lockName));
        Assert.IsTrue(await first.EvaluateAsync<bool>($"async name => ({Import}).isLockHeld(name)", lockName));

        // The other tab of the same origin loses the election while the first one holds the lock...
        Assert.IsFalse(await second.EvaluateAsync<bool>($"async name => ({Import}).acquireLock(name, false)", lockName));
        Assert.IsFalse(await second.EvaluateAsync<bool>($"async name => ({Import}).isLockHeld(name)", lockName));

        // ...and wins it once the first one lets go.
        await first.EvaluateAsync($"async name => ({Import}).releaseLock(name)", lockName);

        Assert.IsTrue(await second.EvaluateAsync<bool>($"async name => ({Import}).acquireLock(name, false)", lockName));
        Assert.IsFalse(await first.EvaluateAsync<bool>($"async name => ({Import}).isLockHeld(name)", lockName));
    }

    [TestMethod]
    public async Task WebCrypto_seals_and_opens_a_frame_and_refuses_a_tampered_one()
    {
        var page = await OpenAsync("/");

        var result = await page.EvaluateAsync<string[]>(
            """
            async () => {
              const aes = await import('./_content/Bit.BlazeDb.Browser/bit-blazedb-crypto.js');
              const key = await aes.importKey(crypto.getRandomValues(new Uint8Array(32)));
              const nonce = crypto.getRandomValues(new Uint8Array(12));
              const ad = new TextEncoder().encode('wal-1.blz');

              const sealed = await aes.encrypt(key, nonce, new TextEncoder().encode('secret row'), ad);
              const opened = new TextDecoder().decode(await aes.decrypt(key, nonce, sealed, ad));

              const tampered = sealed.slice();
              tampered[0] ^= 1;
              let refused = 'accepted';
              try { await aes.decrypt(key, nonce, tampered, ad); } catch (e) { refused = e.name; }

              let moved = 'accepted';
              try { await aes.decrypt(key, nonce, sealed, new TextEncoder().encode('snapshot-1.blz')); } catch (e) { moved = e.name; }

              return [opened, String(sealed.length - 'secret row'.length), refused, moved];
            }
            """);

        // A 128-bit tag on top of the plaintext; a flipped bit or another file's associated data fails authentication.
        CollectionAssert.AreEqual(new[] { "secret row", "16", "OperationError", "OperationError" }, result);
    }

    [TestMethod]
    public async Task A_checkpoint_announcement_reaches_the_other_tabs_and_not_the_sender()
    {
        var context = await NewContextAsync();

        var writer = await context.NewPageAsync();
        await GotoAsync(writer, "/mcp-server");
        var replica = await context.NewPageAsync();
        await GotoAsync(replica, "/mcp-server");

        var channel = $"e2e-{Guid.NewGuid():N}";

        // Each tab records what it hears on its own handle; the writer listens too, to prove a channel does
        // not echo a message back to the object that posted it.
        foreach (var page in new[] { writer, replica })
        {
            await page.EvaluateAsync(
                """
                async channel => {
                  const tabs = await import('./_content/Bit.BlazeDb.Browser/bit-blazedb-tabs.js');
                  window.__heard = [];
                  tabs.open('e2e', channel, generation => window.__heard.push(generation));
                }
                """, channel);
        }

        await writer.EvaluateAsync("async () => (await import('./_content/Bit.BlazeDb.Browser/bit-blazedb-tabs.js')).post('e2e', 7)");

        await replica.WaitForFunctionAsync("() => window.__heard.length > 0", null, new PageWaitForFunctionOptions { Timeout = 10_000 });

        CollectionAssert.AreEqual(new[] { 7 }, await replica.EvaluateAsync<int[]>("() => window.__heard"));
        CollectionAssert.AreEqual(Array.Empty<int>(), await writer.EvaluateAsync<int[]>("() => window.__heard"));
        Assert.IsTrue(await writer.EvaluateAsync<bool>("async () => (await import('./_content/Bit.BlazeDb.Browser/bit-blazedb-tabs.js')).isSupported()"));
    }
}
