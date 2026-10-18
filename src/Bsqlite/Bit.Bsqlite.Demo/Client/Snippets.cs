namespace Bit.Bsqlite.Demo.Client;

/// <summary>
/// The code shown next to each demo. Kept in one place, and kept honest: every snippet is the
/// real call the page above it makes, trimmed only of demo plumbing (busy flags, try/catch).
/// </summary>
internal static class Snippets
{
    public static class Setup
    {
        public const string Install =
            """
            dotnet add package Bit.Bsqlite
            dotnet add package Bit.Bsqlite.Storage.Opfs
            dotnet add package Bit.Bsqlite.Storage.IndexedDb
            dotnet add package Bit.Bsqlite.Storage.CacheStorage
            """;

        public const string Registration =
            """
            // Program.cs - Blazor WebAssembly. One call registers selection, sticky bindings in
            // localStorage, the worker transport, and the shared database.
            builder.Services.AddBitBsqliteServices(sqlite => sqlite
                .AddStorageProvider<BsqliteOpfsStorageProvider>()
                .AddStorageProvider<BsqliteIndexedDbStorageProvider>()
                .AddStorageProvider<BsqliteCacheStorageProvider>()
                .AddStorageProvider<BsqliteInMemoryStorageProvider>()
                .UseStorage(s => s
                    .Prefer(BsqliteOpfsStorageProvider.ProviderName)
                    .Fallback(BsqliteIndexedDbStorageProvider.ProviderName)
                    .Fallback(BsqliteCacheStorageProvider.ProviderName)
                    .Fallback(BsqliteInMemoryStorageProvider.ProviderName)
                    .AllowNonPersistentFallback())
                .AddDatabase("app.db"));

            // Contexts come from a factory bound to that database. Migrations run once per
            // session, then the demo catalog is seeded if the database is empty.
            builder.Services.AddBitBsqliteDbContext<AppDbContext>(options =>
                options.OnFirstUse = (db, ct) => DemoData.SeedIfEmptyAsync(db, ct));
            """;

        /// <summary>The condensed version on the landing page: register, open, query.</summary>
        public const string Hero =
            """
            // Program.cs - one preference order, one database, one context factory.
            builder.Services.AddBitBsqliteServices(sqlite => sqlite
                .AddStorageProvider<BsqliteOpfsStorageProvider>()
                .AddStorageProvider<BsqliteIndexedDbStorageProvider>()
                .UseStorage(s => s.Prefer("opfs").Fallback("indexeddb"))
                .AddDatabase("app.db"));
            builder.Services.AddBitBsqliteDbContext<AppDbContext>();

            // Anywhere you would have opened a DbContext. Migrations ran on first use.
            await using var context = await contexts.CreateDbContextAsync();

            // From here it is ordinary EF Core - running inside the browser tab.
            var open = await context.Orders
                .Include(o => o.Customer)
                .Where(o => o.Status == OrderStatus.Draft)
                .ToListAsync();
            """;

        public const string OpenContext =
            """
            // One session per tab, owned by the registered BsqliteDatabase: it holds the
            // worker, the transport, and the ADO.NET connection. The factory opens it on first use
            // and runs the same migrations that run against the server database, once per session.
            await using var context = await contexts.CreateDbContextAsync(cancellationToken);
            """;
    }

    public static class Home
    {
        public const string Counts =
            """
            await using var context = await Database.OpenContextAsync();

            _products = await context.Products.CountAsync();
            _customers = await context.Customers.CountAsync();
            _openOrders = await context.Orders.CountAsync(o =>
                o.Status != OrderStatus.Shipped && o.Status != OrderStatus.Cancelled);
            """;

        public const string LiveQuery =
            """
            // Table-level: any write to Products re-runs this, including from another tab
            // (BroadcastChannel), including from raw SQL that never touched EF.
            _live = context.Products
                .AsNoTracking()
                .OrderBy(p => p.Sku)
                .Take(8)
                .AsLiveQuery();

            _live.Changed += (_, rows) =>
            {
                _liveRows = rows;
                _ = InvokeAsync(StateHasChanged);
            };

            _liveRows = await _live.RefreshAsync();

            // Disposing the live query unsubscribes it from the connection's change feed.
            await _live.DisposeAsync();
            """;

        public const string Entity =
            """
            public sealed class Product
            {
                public int Id { get; set; }
                public Guid PublicId { get; set; } = Guid.NewGuid();   // TEXT
                public string Sku { get; set; } = string.Empty;        // TEXT
                public decimal Price { get; set; }                     // TEXT + ef_compare
                public double WeightKg { get; set; }                   // REAL
                public bool IsActive { get; set; } = true;             // INTEGER 0/1
                public DateTime CreatedUtc { get; set; }               // TEXT, ISO 8601
                public DateOnly? DiscontinuedOn { get; set; }          // TEXT date, nullable
                public TimeSpan LeadTime { get; set; }                 // TEXT, "c" format
                public int CategoryId { get; set; }
                public Category? Category { get; set; }
            }
            """;
    }

    public static class Catalog
    {
        public const string Read =
            """
            _categories = await context.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();

            _products = await context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .OrderBy(p => p.Sku)
                .ToListAsync();
            """;

        public const string Write =
            """
            product.LeadTime = TimeSpan.FromDays(_leadDays);
            if (!product.IsActive && product.DiscontinuedOn is null)
            {
                product.DiscontinuedOn = DateOnly.FromDateTime(DateTime.UtcNow);
            }

            if (product.Id == 0)
            {
                context.Products.Add(product);
            }
            else
            {
                context.Products.Update(product);
            }

            // Async only. SaveChanges() throws BsqliteSynchronousApiNotSupportedException.
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            """;

        public const string Delete =
            """
            // OrderLine -> Product is DeleteBehavior.Restrict, so deleting a product that is on an
            // order raises SQLITE_CONSTRAINT_FOREIGNKEY inside the worker and surfaces as a
            // DbUpdateException here - foreign keys are enforced, not simulated.
            var tracked = await context.Products.FirstAsync(p => p.Id == product.Id);
            context.Products.Remove(tracked);
            await context.SaveChangesAsync();
            """;

        public const string Configuration =
            """
            public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
            {
                public void Configure(EntityTypeBuilder<Product> builder)
                {
                    builder.HasKey(p => p.Id);
                    builder.HasIndex(p => p.PublicId).IsUnique();
                    builder.Property(p => p.Sku).IsRequired().HasMaxLength(32);
                    builder.HasIndex(p => p.Sku).IsUnique();
                    builder.Property(p => p.Price).HasPrecision(18, 2);
                    builder.HasOne(p => p.Category)
                        .WithMany(c => c.Products)
                        .HasForeignKey(p => p.CategoryId)
                        .OnDelete(DeleteBehavior.Cascade);
                }
            }
            """;
    }

    public static class Customers
    {
        public const string Entity =
            """
            public sealed class Customer
            {
                public int Id { get; set; }
                public Guid PublicId { get; set; } = Guid.NewGuid();
                public string DisplayName { get; set; } = string.Empty;
                public string Email { get; set; } = string.Empty;
                public DateOnly DateOfBirth { get; set; }
                public bool IsVip { get; set; }
                public decimal CreditLimit { get; set; }
                public string Notes { get; set; } = string.Empty;
                public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
            }

            // Precision travels with the model, not with the backend.
            builder.Property(c => c.CreditLimit).HasPrecision(18, 2);
            builder.HasIndex(c => c.Email).IsUnique();
            """;

        public const string Crud =
            """
            _rows = await context.Customers
                .AsNoTracking()
                .OrderBy(c => c.DisplayName)
                .ToListAsync();

            // Insert: PublicId is a Guid the CLR generated, stored as TEXT and read back as a Guid.
            context.Customers.Add(new Customer
            {
                DisplayName = "Ada Lovelace",
                Email = "ada@example.com",
                DateOfBirth = new DateOnly(1980, 1, 1),
                CreditLimit = 1_000m,
                IsVip = true,
            });

            await context.SaveChangesAsync();
            """;

        public const string Unique =
            """
            // The unique index on Email is enforced by SQLite in the worker. A duplicate throws
            // DbUpdateException wrapping BsqliteException with the SQLite result code.
            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _error = ex.InnerException?.Message ?? ex.Message;
                context.ChangeTracker.Clear();
            }
            """;
    }

    public static class Orders
    {
        public const string Read =
            """
            _orders = await context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Lines)
                    .ThenInclude(l => l.Product)
                .OrderByDescending(o => o.Id)
                .ToListAsync();
            """;

        public const string Configuration =
            """
            public sealed class SalesOrderConfiguration : IEntityTypeConfiguration<SalesOrder>
            {
                public void Configure(EntityTypeBuilder<SalesOrder> builder)
                {
                    builder.ToTable("Orders");
                    builder.Property(o => o.Number).IsRequired().HasMaxLength(24);
                    builder.HasIndex(o => o.Number).IsUnique();

                    // Stored as TEXT: readable in an exported file, stable across enum reordering.
                    builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(16);

                    builder.HasOne(o => o.Customer)
                        .WithMany(c => c.Orders)
                        .HasForeignKey(o => o.CustomerId)
                        .OnDelete(DeleteBehavior.Restrict);
                }
            }
            """;

        public const string Graph =
            """
            // Header first: the line items need the generated key.
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            context.OrderLines.Add(new OrderLine
            {
                OrderId = order.Id,
                ProductId = product.Id,
                Quantity = quantity,
                UnitPrice = product.Price,   // price is captured at line time
            });

            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            """;

        public const string Offset =
            """
            // DateTimeOffset round-trips as TEXT with its offset, but SQLite cannot compare it,
            // so the stock SQLite provider refuses ORDER BY on it. Sort by the key instead:
            .OrderByDescending(o => o.Id)

            // ...and do offset-sensitive work in the CLR after materialising:
            var latest = _orders.MaxBy(o => o.OrderedAt);
            """;
    }

    public static class Storage
    {
        public const string Selection =
            """
            BsqliteStorageSelectionBuilder.Create(s => s
                .Prefer(BsqliteOpfsStorageProvider.ProviderName)
                .Fallback(BsqliteIndexedDbStorageProvider.ProviderName)
                .Fallback(BsqliteCacheStorageProvider.ProviderName)
                .Fallback(BsqliteInMemoryStorageProvider.ProviderName)
                .AllowNonPersistentFallback());   // without this, in-memory is not acceptable

            // Default is BsqliteStorageMigrationMode.KeepExisting: data outranks preference.
            // AutomaticOnOpen copies to the preferred backend, checks the image, then rebinds.
            """;

        public const string Resolution =
            """
            var session = await database.OpenAsync();
            var resolution = session.Resolution;

            resolution.Provider.Name;                 // the backend that actually opened
            resolution.IsFirstChoice;                 // false when a fallback won
            resolution.WasDecidedByExistingData;      // the sticky binding overruled preference
            resolution.BetterProviderAvailable?.Name; // ranked higher but holds no data
            resolution.Attempts;                      // every candidate, with the reason it lost
            """;

        public const string Probe =
            """
            foreach (var provider in Database.Providers)
            {
                var probe = await provider.ProbeAsync();

                probe.IsAvailable;         // unavailability is a result, never an exception
                probe.UnavailableReason;   // why, in one sentence
                probe.UsageBytes;          // when the browser will say
                probe.QuotaBytes;
                probe.Environment;         // raw facts: JSPI, getDirectory, isSecureContext...
            }
            """;

        public const string Capabilities =
            """
            var caps = session.Resolution.Provider.Capabilities;

            caps.IsPersistent;                       // survives reload
            caps.SupportsMultipleConnections;
            caps.SupportsConcurrentReads;
            caps.SupportsRelaxedDurability;
            caps.SupportsMultiDatabaseTransactions;  // gates ATTACH
            caps.CanChangePageSize;                  // gates PRAGMA page_size

            // The core reads these flags. It never special-cases a provider by name, so a new
            // backend gets the same guards without touching Bsqlite.
            """;

        public const string Persistence =
            """
            // navigator.storage.persist(). Origin-wide, not per backend: without it the browser
            // may evict OPFS / IndexedDB / Cache Storage under pressure.
            bool granted = await BsqlitePersistence.RequestAsync(js);
            """;

        public const string Migrate =
            """
            // Copy the image to another backend: probe target -> quota headroom -> export ->
            // import -> SQLite header check -> flip the sticky binding -> delete the source.
            // A failure anywhere leaves both the source and the binding untouched.
            await new BsqliteStorageMigrator()
                .MigrateAsync("app.db", source, target, bindingStore, cancellationToken);
            """;
    }

    public static class Sql
    {
        public const string LiveInclude =
            """
            _live = context.Orders
                .Include(o => o.Customer)
                .OrderByDescending(o => o.Id)
                .AsLiveQuery();

            _live.Changed += (_, rows) =>
            {
                _liveRows = rows;
                _ = InvokeAsync(StateHasChanged);
            };

            _liveRows = await _live.RefreshAsync();
            """;

        public const string QueryString =
            """
            var query = context.Customers
                .AsNoTracking()
                .Where(c => c.CreditLimit >= _minCredit)
                .OrderBy(c => c.DisplayName);

            _decimalSql = query.ToQueryString();   // the SQL below, straight from EF
            _decimalRows = await query.ToListAsync();
            """;

        public const string Regexp =
            """
            // regexp is registered on every open, so raw SQL can use it too.
            _regexRows = await context.Customers
                .FromSql($"SELECT * FROM \"Customers\" WHERE \"Email\" REGEXP {_pattern}")
                .AsNoTracking()
                .ToListAsync();
            """;

        public const string Ado =
            """
            // No EF: the session's connection is a DbConnection, already open.
            await using var command = session.Connection.CreateCommand();
            command.CommandText = "SELECT sqlite_version()";
            var version = await command.ExecuteScalarAsync(cancellationToken);

            // Parameters bind by CLR type; the binder picks TEXT / INTEGER / REAL / BLOB.
            command.CommandText = "SELECT * FROM Products WHERE Price >= $min";
            command.Parameters.Add(new BsqliteParameter { ParameterName = "$min", Value = 25.00m });
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var sku = reader.GetString(reader.GetOrdinal("Sku"));
            }
            """;

        public const string Transaction =
            """
            await using var transaction = await session.Connection.BeginTransactionAsync(ct);
            try
            {
                await context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
            """;
    }

    public static class Admin
    {
        public const string List =
            """
            // Databases as files, outside any SQL connection. No EF involved.
            foreach (var provider in Database.Providers)
            {
                IReadOnlyList<string> names = await provider.Admin.ListAsync();
            }
            """;

        public const string Export =
            """
            var session = await Database.EnsureSessionAsync();
            var provider = session.Resolution.Provider;

            byte[] image = await provider.Admin.ExportAsync("app.db");

            // A plain SQLite file: open it in any desktop tool.
            await js.InvokeVoidAsync("bsqliteDemo.download", "app.db", image);
            """;

        public const string Import =
            """
            // Close the worker first so it cannot keep writing the image being replaced.
            await Database.CloseSessionAsync();

            await provider.Admin.ImportAsync("app.db", bytes);

            // Record the sticky binding, or the next open would create an empty file on the
            // preferred backend and the import would look like it vanished.
            await Database.Bindings.SetProviderNameAsync("app.db", provider.Name);

            // Reopen once so migrations run before the pages come back for the database. The
            // open raises Opened, which is how every page hears it should reload.
            await using (await Database.OpenContextAsync())
            {
            }
            """;

        public const string Delete =
            """
            await Database.CloseSessionAsync();
            await provider.Admin.DeleteAsync("app.db");

            // Forget the binding so selection is free to pick the preferred backend again.
            await Database.Bindings.RemoveAsync("app.db");

            // Migrates and reseeds the now-empty database.
            await using (await Database.OpenContextAsync())
            {
            }
            """;
    }

    public static class Limits
    {
        public const string Sync =
            """
            await using var context = await Database.OpenContextAsync();

            // EF short-circuits SaveChanges() when nothing is pending, so stage a real insert
            // first - otherwise the call returns 0 and never reaches the connection.
            context.Categories.Add(new Category { Name = "sync-probe", Color = "#0f766e" });

            context.SaveChanges();
            // DbUpdateException -> BsqliteSynchronousApiNotSupportedException:
            //   the engine is in a worker and the Blazor UI thread cannot block on it.
            //   Use SaveChangesAsync(). Same for ToList(), First(), DbConnection.Open().
            //
            // EF wraps save failures, so read InnerException for the message that explains it.
            """;

        public const string Wal =
            """
            await Database.ExecuteNonQueryAsync("PRAGMA journal_mode=WAL");
            // No web VFS implements WAL: WASM has no shared-memory primitives for it, and the
            // official SQLite WASM build cannot even read a WAL-mode file.
            // Journal mode is DELETE or TRUNCATE. Concurrent reads are a VFS capability instead:
            //   session.Resolution.Provider.Capabilities.SupportsConcurrentReads
            """;

        public const string Attach =
            """
            var limits = session.Connection.RuntimeLimits;
            if (!limits.SupportsMultiDatabaseTransactions)
            {
                // Refused before it runs, by capability - not by provider name.
                return;
            }

            await Database.ExecuteNonQueryAsync("ATTACH DATABASE 'other.db' AS other");
            """;

        public const string PageSize =
            """
            var limits = session.Connection.RuntimeLimits;
            limits.CanChangePageSize;   // false on block-oriented backends

            // Letting PRAGMA page_size through on a backend that pins it would report success and
            // then corrupt the image, so BsqliteFeatureGuards throws instead.
            await Database.ExecuteNonQueryAsync("PRAGMA page_size=512");
            """;
    }
}
