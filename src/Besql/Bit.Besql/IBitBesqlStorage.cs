namespace Bit.Besql;

public interface IBitBesqlStorage
{   
    /// <summary>
    /// When you're running in a WebAssembly environment and working with an SQLite database (using APIs like System.IO.File or EF Core's DbContext),
    /// the database file is loaded into memory by emscripten. <see cref="Persist"/> method takes that in-memory file and saves it into the browser's cache storage,
    /// ensuring your data sticks around even after a page refresh.
    /// Note: In Blazor Hybrid and Server, this method doesn't actually do anything (check out <see cref="BitBesqlNoopStoage"/>).
    /// </summary>
    Task Persist(string filename);

    /// <summary>
    /// At application startup, this method fetches the SQLite database file from the browser's cache storage (which was saved via <see cref="Persist"/>)
    /// and loads it into in-memory memory. This way, EF Core and other .NET APIs like System.IO.File can interact with the database as expected.
    /// Note: In Blazor Hybrid and Server, this method doesn't actually do anything (check out <see cref="BitBesqlNoopStoage"/>).
    /// </summary>
    Task Load(string filename);

    /// <summary>
    /// Typically, whenever you modify the SQLite database using EF Core, the bit Besql automatically calls <see cref="Persist"/> to sync those changes
    /// to the browser's cache storage. This sync is throttled (so multiple SaveChangeAsync calls might lead to just one sync).
    /// If you're planning on making a bunch of changes in quick succession, you might want to pause this automatic syncing.
    /// Simply call <see cref="PauseAutomaticPersistent"/> to hold off syncing, and don't forget to resume with
    /// <see cref="ResumeAutomaticPersistent"/> once you're done.
    /// Note: In Blazor Hybrid and Server, this method doesn't actually do anything (check out <see cref="BitBesqlNoopStoage"/>).
    /// </summary>
    void PauseAutomaticPersistent();

    /// <summary>
    /// This method resumes the syncing process that you paused with <see cref="PauseAutomaticPersistent"/>.
    /// It gets your automatic syncing back on track, ensuring that your database changes continue to be saved to the browser's cache storage.
    /// Note: In Blazor Hybrid and Server, this method doesn't actually do anything (check out <see cref="BitBesqlNoopStoage"/>).
    /// </summary>
    Task ResumeAutomaticPersistent();
}
