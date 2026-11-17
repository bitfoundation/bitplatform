namespace Bit.Bsqlite.Interop.Tests;

/// <summary>
/// The browser VFSes key a database by <c>new URL(name, 'file://').pathname</c>, which is not one to one. A name is
/// kept only when it reads back from its key the way the OPFS admin lists it, so no two names share a file - and a
/// name that does read back, '%' and all, keeps working.
/// </summary>
[TestClass]
public sealed class DatabaseNamesTests
{
    [TestMethod]
    [DataRow("app.db")]
    [DataRow("My Notes.db")]
    [DataRow("é.db")]
    [DataRow("App_Data/AppOffline.db")]
    [DataRow("50%off.db")]
    [DataRow("a%41.db")]
    [DataRow("a%2Fb.db")]
    [DataRow("a%c3%a9.db")]
    [DataRow("a^b.db")]
    [DataRow("a|b.db")]
    [DataRow("1tenant:1.db")]
    public void ANameThatReadsBackFromItsKey_IsKept(string databaseName)
        => Assert.IsNull(BsqliteDatabaseNames.WhyInvalidInBrowser(databaseName));

    [TestMethod]
    [DataRow("tenant#1.db")]
    [DataRow("tenant?1.db")]
    [DataRow("tenant%201.db")]
    [DataRow("a%C3%A9.db")]
    [DataRow("a%5Eb.db")]
    [DataRow("a\tb.db")]
    [DataRow("a\nb.db")]
    [DataRow("a\\b.db")]
    [DataRow("x/../app.db")]
    [DataRow("./app.db")]
    [DataRow("x/%2e%2E/app.db")]
    [DataRow("/app.db")]
    [DataRow("a//b.db")]
    [DataRow("dir/")]
    [DataRow(" app.db")]
    [DataRow("app.db ")]
    [DataRow("tenant:1.db")]
    [DataRow("c:app.db")]
    [DataRow("file:app.db")]
    [DataRow("c|/app.db")]
    public void ANameThatSharesItsKey_IsRefused(string databaseName)
    {
        Assert.IsNotNull(BsqliteDatabaseNames.WhyInvalidInBrowser(databaseName));
        var error = Assert.ThrowsExactly<ArgumentException>(
            () => BsqliteDatabaseNames.ThrowIfInvalidInBrowser(databaseName, "databaseName"));
        Assert.AreEqual("databaseName", error.ParamName);
    }

    /// <summary>
    /// The browser admin modules trim the name with JavaScript's <c>trim()</c> and the VFS keys it untrimmed, so
    /// '\u00A0app.db' opened the file '%C2%A0app.db' while an export, import or delete of it reached 'app.db' - another
    /// database. The untrimmed name is refused; its trimmed counterpart, the one the admin surface addresses, is kept.
    /// </summary>
    [TestMethod]
    [DataRow("\u00A0app.db")]
    [DataRow("app.db\u00A0")]
    [DataRow("\uFEFFapp.db")]
    [DataRow("\u3000app.db")]
    [DataRow("\u2003app.db")]
    [DataRow("app.db\u2028")]
    public void ANameJavaScriptTrims_IsRefused_AndItsTrimmedCounterpartKept(string databaseName)
    {
        Assert.IsNotNull(BsqliteDatabaseNames.WhyInvalidInBrowser(databaseName));
        Assert.ThrowsExactly<ArgumentException>(() => BsqliteDatabaseNames.ThrowIfInvalidInBrowser(databaseName));
        Assert.IsNull(BsqliteDatabaseNames.WhyInvalidInBrowser("app.db"));
    }

    /// <summary>
    /// A lone surrogate is written as U+FFFD's escape, the key of a name spelled with U+FFFD. Built here rather than
    /// in a DataRow, whose metadata cannot hold a lone surrogate and stores U+FFFD in its place.
    /// </summary>
    [TestMethod]
    public void ANameWithALoneSurrogate_IsRefused()
    {
        var databaseName = "a" + (char)0xD800 + ".db";

        Assert.IsNotNull(BsqliteDatabaseNames.WhyInvalidInBrowser(databaseName));
        Assert.IsNull(BsqliteDatabaseNames.WhyInvalidInBrowser("a�.db"));
    }
}
