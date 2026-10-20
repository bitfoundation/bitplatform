using SQLitePCL;

namespace Bit.Bsqlite.EntityFrameworkCore;

/// <summary>
/// The no-op <c>SQLitePCL.raw</c> provider <see cref="BsqlitePcl"/> installs on WASM, so
/// EF Core can read a version without a native e_sqlite3 behind it.
/// </summary>
internal sealed class BsqlitePclProvider : ISQLite3Provider
{
    /// <summary>
    /// The SQLite version inside the vendored wa-sqlite build. Must match what the engine reports;
    /// see <c>engine/wa-sqlite.lock.props</c> for the pinned build, and the browser suite's
    /// engine-version test, which fails if the two drift apart.
    /// </summary>
    /// <remarks>
    /// This is the number EF Core gates its SQL translations on, so a wrong one is not cosmetic.
    /// Claiming less than the engine offers makes EF fall back to older SQL in the browser while it
    /// emits the newer form on the server - the same model producing two different queries,
    /// silently. Claiming more makes it emit SQL the engine cannot run. Reporting the truth also
    /// happens to be what keeps the two halves in step: the engine SQLitePCLRaw bundles for EF 10
    /// is 3.53.x, the same series as the vendored wa-sqlite build.
    /// </remarks>
    internal const string EngineVersion = "3.53.0";

    private const int EngineVersionNumber = 3_053_000;

    public string GetNativeLibraryName() => "bit-bsqlite";

    public utf8z sqlite3_libversion() => utf8z.FromString(EngineVersion);

    public int sqlite3_libversion_number() => EngineVersionNumber;

    public utf8z sqlite3_sourceid() => utf8z.FromString("bit-bsqlite");

    public int sqlite3_threadsafe() => 1;

    public int sqlite3_initialize() => 0;

    public int sqlite3_shutdown() => 0;

    public int sqlite3_win32_set_directory(int typ, utf8z path) => 0;

    public int sqlite3_keyword_count() => 0;

    public int sqlite3_keyword_name(int i, out string name)
    {
        name = string.Empty;
        return 0;
    }

    // Everything below is never reached: EF Core asks this provider for its version and nothing
    // else, and no SQLite handle is ever opened through it. Each member returns the neutral value
    // of its type so the interface is satisfied without a native library.
    public int sqlite3_open(utf8z filename, out IntPtr db)
    {
        db = IntPtr.Zero;
        return 0;
    }
    public int sqlite3_open_v2(utf8z filename, out IntPtr db, int flags, utf8z vfs)
    {
        db = IntPtr.Zero;
        return 0;
    }
    public int sqlite3_close_v2(IntPtr db) => 0;
    public int sqlite3_close(IntPtr db) => 0;
    public int sqlite3_enable_shared_cache(int enable) => 0;
    public void sqlite3_interrupt(sqlite3 db) { }
    public int sqlite3__vfs__delete(utf8z vfs, utf8z pathname, int syncDir) => 0;
    public long sqlite3_memory_used() => 0;
    public long sqlite3_memory_highwater(int resetFlag) => 0;
    public long sqlite3_soft_heap_limit64(long n) => 0;
    public long sqlite3_hard_heap_limit64(long n) => 0;
    public int sqlite3_status(int op, out int current, out int highwater, int resetFlag)
    {
        current = 0;
        highwater = 0;
        return 0;
    }
    public int sqlite3_db_readonly(sqlite3 db, utf8z dbName) => 0;
    public utf8z sqlite3_db_filename(sqlite3 db, utf8z att) => default;
    public utf8z sqlite3_errmsg(sqlite3 db) => default;
    public long sqlite3_last_insert_rowid(sqlite3 db) => 0;
    public int sqlite3_changes(sqlite3 db) => 0;
    public int sqlite3_total_changes(sqlite3 db) => 0;
    public int sqlite3_get_autocommit(sqlite3 db) => 0;
    public int sqlite3_busy_timeout(sqlite3 db, int ms) => 0;
    public int sqlite3_extended_result_codes(sqlite3 db, int onoff) => 0;
    public int sqlite3_errcode(sqlite3 db) => 0;
    public int sqlite3_extended_errcode(sqlite3 db) => 0;
    public utf8z sqlite3_errstr(int rc) => default;
    public int sqlite3_prepare_v2(sqlite3 db, ReadOnlySpan<byte> sql, out IntPtr stmt, out ReadOnlySpan<byte> remain)
    {
        stmt = IntPtr.Zero;
        remain = default;
        return 0;
    }
    public int sqlite3_prepare_v3(sqlite3 db, ReadOnlySpan<byte> sql, uint flags, out IntPtr stmt, out ReadOnlySpan<byte> remain)
    {
        stmt = IntPtr.Zero;
        remain = default;
        return 0;
    }

    [Obsolete]
    public int sqlite3_prepare_v2(sqlite3 db, utf8z sql, out IntPtr stmt, out utf8z remain)
    {
        stmt = IntPtr.Zero;
        remain = default;
        return 0;
    }

    [Obsolete]
    public int sqlite3_prepare_v3(sqlite3 db, utf8z sql, uint flags, out IntPtr stmt, out utf8z remain)
    {
        stmt = IntPtr.Zero;
        remain = default;
        return 0;
    }
    public int sqlite3_step(sqlite3_stmt stmt) => 0;
    public int sqlite3_finalize(IntPtr stmt) => 0;
    public int sqlite3_reset(sqlite3_stmt stmt) => 0;
    public int sqlite3_clear_bindings(sqlite3_stmt stmt) => 0;
    public int sqlite3_stmt_status(sqlite3_stmt stmt, int op, int resetFlg) => 0;
    public utf8z sqlite3_sql(sqlite3_stmt stmt) => default;
    public IntPtr sqlite3_db_handle(IntPtr stmt) => IntPtr.Zero;
    public IntPtr sqlite3_next_stmt(sqlite3 db, IntPtr stmt) => IntPtr.Zero;
    public int sqlite3_bind_zeroblob(sqlite3_stmt stmt, int index, int size) => 0;
    public utf8z sqlite3_bind_parameter_name(sqlite3_stmt stmt, int index) => default;
    public int sqlite3_bind_blob(sqlite3_stmt stmt, int index, ReadOnlySpan<byte> blob) => 0;
    public int sqlite3_bind_double(sqlite3_stmt stmt, int index, double val) => 0;
    public int sqlite3_bind_int(sqlite3_stmt stmt, int index, int val) => 0;
    public int sqlite3_bind_int64(sqlite3_stmt stmt, int index, long val) => 0;
    public int sqlite3_bind_null(sqlite3_stmt stmt, int index) => 0;
    public int sqlite3_bind_text(sqlite3_stmt stmt, int index, ReadOnlySpan<byte> text) => 0;
    public int sqlite3_bind_text16(sqlite3_stmt stmt, int index, ReadOnlySpan<char> text) => 0;
    public int sqlite3_bind_text(sqlite3_stmt stmt, int index, utf8z text) => 0;
    public int sqlite3_bind_parameter_count(sqlite3_stmt stmt) => 0;
    public int sqlite3_bind_parameter_index(sqlite3_stmt stmt, utf8z strName) => 0;
    public utf8z sqlite3_column_database_name(sqlite3_stmt stmt, int index) => default;
    public utf8z sqlite3_column_name(sqlite3_stmt stmt, int index) => default;
    public utf8z sqlite3_column_origin_name(sqlite3_stmt stmt, int index) => default;
    public utf8z sqlite3_column_table_name(sqlite3_stmt stmt, int index) => default;
    public utf8z sqlite3_column_text(sqlite3_stmt stmt, int index) => default;
    public int sqlite3_data_count(sqlite3_stmt stmt) => 0;
    public int sqlite3_column_count(sqlite3_stmt stmt) => 0;
    public double sqlite3_column_double(sqlite3_stmt stmt, int index) => 0;
    public int sqlite3_column_int(sqlite3_stmt stmt, int index) => 0;
    public long sqlite3_column_int64(sqlite3_stmt stmt, int index) => 0;
    public ReadOnlySpan<byte> sqlite3_column_blob(sqlite3_stmt stmt, int index) => default;
    public int sqlite3_column_bytes(sqlite3_stmt stmt, int index) => 0;
    public int sqlite3_column_type(sqlite3_stmt stmt, int index) => 0;
    public utf8z sqlite3_column_decltype(sqlite3_stmt stmt, int index) => default;
    public int sqlite3_snapshot_get(sqlite3 db, utf8z schema, out IntPtr snap)
    {
        snap = IntPtr.Zero;
        return 0;
    }
    public int sqlite3_snapshot_cmp(sqlite3_snapshot p1, sqlite3_snapshot p2) => 0;
    public int sqlite3_snapshot_open(sqlite3 db, utf8z schema, sqlite3_snapshot snap) => 0;
    public int sqlite3_snapshot_recover(sqlite3 db, utf8z name) => 0;
    public void sqlite3_snapshot_free(IntPtr snap) { }
    public sqlite3_backup sqlite3_backup_init(sqlite3 destDb, utf8z destName, sqlite3 sourceDb, utf8z sourceName) => null!;
    public int sqlite3_backup_step(sqlite3_backup backup, int nPage) => 0;
    public int sqlite3_backup_remaining(sqlite3_backup backup) => 0;
    public int sqlite3_backup_pagecount(sqlite3_backup backup) => 0;
    public int sqlite3_backup_finish(IntPtr backup) => 0;
    public int sqlite3_blob_open(sqlite3 db, utf8z db_utf8, utf8z table_utf8, utf8z col_utf8, long rowid, int flags, out sqlite3_blob blob)
    {
        blob = null!;
        return 0;
    }
    public int sqlite3_blob_bytes(sqlite3_blob blob) => 0;
    public int sqlite3_blob_reopen(sqlite3_blob blob, long rowid) => 0;
    public int sqlite3_blob_write(sqlite3_blob blob, ReadOnlySpan<byte> b, int offset) => 0;
    public int sqlite3_blob_read(sqlite3_blob blob, Span<byte> b, int offset) => 0;
    public int sqlite3_blob_close(IntPtr blob) => 0;
    public int sqlite3_config_log(delegate_log func, object v) => 0;
    public void sqlite3_log(int errcode, utf8z s) { }
    public void sqlite3_commit_hook(sqlite3 db, delegate_commit func, object v) { }
    public void sqlite3_rollback_hook(sqlite3 db, delegate_rollback func, object v) { }
    public void sqlite3_trace(sqlite3 db, delegate_trace func, object v) { }
    public void sqlite3_profile(sqlite3 db, delegate_profile func, object v) { }
    public void sqlite3_progress_handler(sqlite3 db, int instructions, delegate_progress func, object v) { }
    public void sqlite3_update_hook(sqlite3 db, delegate_update func, object v) { }
    public int sqlite3_create_collation(sqlite3 db, byte[] name, object v, delegate_collation func) => 0;
    public int sqlite3_create_function(sqlite3 db, byte[] name, int nArg, int flags, object v, delegate_function_scalar func) => 0;
    public int sqlite3_create_function(sqlite3 db, byte[] name, int nArg, int flags, object v, delegate_function_aggregate_step func_step, delegate_function_aggregate_final func_final) => 0;
    public int sqlite3_db_status(sqlite3 db, int op, out int current, out int highest, int resetFlg)
    {
        current = 0;
        highest = 0;
        return 0;
    }
    public void sqlite3_result_blob(IntPtr context, ReadOnlySpan<byte> val) { }
    public void sqlite3_result_double(IntPtr context, double val) { }
    public void sqlite3_result_error(IntPtr context, ReadOnlySpan<byte> strErr) { }
    public void sqlite3_result_error(IntPtr context, utf8z strErr) { }
    public void sqlite3_result_int(IntPtr context, int val) { }
    public void sqlite3_result_int64(IntPtr context, long val) { }
    public void sqlite3_result_null(IntPtr context) { }
    public void sqlite3_result_text(IntPtr context, ReadOnlySpan<byte> val) { }
    public void sqlite3_result_text(IntPtr context, utf8z val) { }
    public void sqlite3_result_zeroblob(IntPtr context, int n) { }
    public void sqlite3_result_error_toobig(IntPtr context) { }
    public void sqlite3_result_error_nomem(IntPtr context) { }
    public void sqlite3_result_error_code(IntPtr context, int code) { }
    public ReadOnlySpan<byte> sqlite3_value_blob(IntPtr p) => default;
    public int sqlite3_value_bytes(IntPtr p) => 0;
    public double sqlite3_value_double(IntPtr p) => 0;
    public int sqlite3_value_int(IntPtr p) => 0;
    public long sqlite3_value_int64(IntPtr p) => 0;
    public int sqlite3_value_type(IntPtr p) => 0;
    public utf8z sqlite3_value_text(IntPtr p) => default;
    public int sqlite3_stmt_isexplain(sqlite3_stmt stmt) => 0;
    public int sqlite3_stmt_busy(sqlite3_stmt stmt) => 0;
    public int sqlite3_stmt_readonly(sqlite3_stmt stmt) => 0;
    public int sqlite3_exec(sqlite3 db, utf8z sql, delegate_exec callback, object user_data, out IntPtr errMsg)
    {
        errMsg = IntPtr.Zero;
        return 0;
    }
    public int sqlite3_complete(utf8z sql) => 0;
    public int sqlite3_compileoption_used(utf8z sql) => 0;
    public utf8z sqlite3_compileoption_get(int n) => default;
    public int sqlite3_wal_autocheckpoint(sqlite3 db, int n) => 0;
    public int sqlite3_wal_checkpoint(sqlite3 db, utf8z dbName) => 0;
    public int sqlite3_wal_checkpoint_v2(sqlite3 db, utf8z dbName, int eMode, out int logSize, out int framesCheckPointed)
    {
        logSize = 0;
        framesCheckPointed = 0;
        return 0;
    }
    public int sqlite3_table_column_metadata(sqlite3 db, utf8z dbName, utf8z tblName, utf8z colName, out utf8z dataType, out utf8z collSeq, out int notNull, out int primaryKey, out int autoInc)
    {
        dataType = default;
        collSeq = default;
        notNull = 0;
        primaryKey = 0;
        autoInc = 0;
        return 0;
    }
    public int sqlite3_set_authorizer(sqlite3 db, delegate_authorizer authorizer, object user_data) => 0;
    public int sqlite3_stricmp(IntPtr p, IntPtr q) => 0;
    public int sqlite3_strnicmp(IntPtr p, IntPtr q, int n) => 0;
    public IntPtr sqlite3_malloc(int n) => IntPtr.Zero;
    public IntPtr sqlite3_malloc64(long n) => IntPtr.Zero;
    public void sqlite3_free(IntPtr p) { }
    public int sqlite3_key(sqlite3 db, ReadOnlySpan<byte> key) => 0;
    public int sqlite3_key_v2(sqlite3 db, utf8z dbname, ReadOnlySpan<byte> key) => 0;
    public int sqlite3_rekey(sqlite3 db, ReadOnlySpan<byte> key) => 0;
    public int sqlite3_rekey_v2(sqlite3 db, utf8z dbname, ReadOnlySpan<byte> key) => 0;
    public int sqlite3_load_extension(sqlite3 db, utf8z zFile, utf8z zProc, out utf8z pzErrMsg)
    {
        pzErrMsg = default;
        return 0;
    }
    public int sqlite3_limit(sqlite3 db, int id, int newVal) => 0;
    public int sqlite3_config(int op) => 0;
    public int sqlite3_config(int op, int val) => 0;
    public int sqlite3_db_config(sqlite3 db, int op, utf8z val) => 0;
    public int sqlite3_db_config(sqlite3 db, int op, int val, out int result)
    {
        result = 0;
        return 0;
    }
    public int sqlite3_db_config(sqlite3 db, int op, IntPtr ptr, int int0, int int1) => 0;
    public int sqlite3_enable_load_extension(sqlite3 db, int enable) => 0;
    public IntPtr sqlite3_serialize(sqlite3 db, utf8z schema, out long size, int flags)
    {
        size = 0;
        return IntPtr.Zero;
    }
    public int sqlite3_deserialize(sqlite3 db, utf8z schema, IntPtr data, long szDb, long szBuf, int flags) => 0;
}
