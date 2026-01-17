namespace Aspire.Hosting.ApplicationModel;

public static class IResourceBuilderExtensions
{
    /// <summary>
    /// Configures PostgreSQL with production-grade tuning optimized for Vector Search (pgvector) using DiskANN.
    /// 
    /// <para><strong>Target Environment:</strong></para>
    /// <list type="bullet">
    ///     <item><strong>Storage:</strong> High-Quality SSD / NVMe (Crucial for DiskANN random access).</item>
    ///     <item><strong>Memory:</strong> ~4GB RAM Container (Balances Shared Buffers vs OS Cache).</item>
    ///     <item><strong>Workload:</strong> Vector Similarity Search + Re-ranking (requires sort memory).</item>
    /// </list>
    /// </summary>
    /// <param name="totalContainerMemoryMB">Total RAM limit allocated to the container (Default: 4096MB).</param>
    public static IResourceBuilder<PostgresServerResource> WithOptimizedSetup(
        this IResourceBuilder<PostgresServerResource> builder,
        int totalContainerMemoryMB = 4 * 1024)
    {
        // SAFETY MARGIN:
        // Reserve 10% or 512MB (whichever is larger) for OS overhead INSIDE the container, 
        // monitoring sidecars, and connection overhead.
        int reservedOverhead = Math.Max(512, (int)(totalContainerMemoryMB * 0.10));

        // Memory available specifically for Postgres Internals + File System Cache
        int availableForPostgres = totalContainerMemoryMB - reservedOverhead;

        // 1. Shared Buffers: 25% of Available RAM (Standard Recommendation)
        // For 4GB total, this will be roughly ~800MB-1GB.
        int sharedBuffersMB = availableForPostgres / 4;

        // 2. Effective Cache Size: 
        // Usually 50-75% of total RAM. Since we already reserved overhead, 
        // we can safely assume the remainder of availableForPostgres is usable for FS cache.
        // DiskANN relies heavily on this to cache the vector graph.
        int effectiveCacheSizeMB = availableForPostgres - sharedBuffersMB;

        // 3. Maintenance Work Mem:
        int maintenanceWorkMemMB = Math.Min(512, availableForPostgres / 10);

        // 4. WAL Settings
        int maxWalSizeGb = Math.Clamp(availableForPostgres / 1024, 1, 4);

        builder
            // --- RUNTIME RESOURCES ---
            // Ensures the container actually gets the RAM we are tuning for.
            .WithContainerRuntimeArgs("--memory-reservation", $"{totalContainerMemoryMB}m");

        return builder
            // --- MEMORY TUNING ---
            .WithArgs("-c", $"shared_buffers={sharedBuffersMB}MB")
            .WithArgs("-c", $"effective_cache_size={effectiveCacheSizeMB}MB")
            .WithArgs("-c", $"maintenance_work_mem={maintenanceWorkMemMB}MB")
            .WithArgs("-c", $"wal_buffers=16MB")

            // --- QUERY & RE-RANKING TUNING ---
            // Standard is 4MB. We bump to 16MB because Re-ranking often involves 
            // sorting/hashing intermediate results. 
            // WARNING: If you have 100+ active concurrent connections, monitor RAM usage.
            .WithArgs("-c", "work_mem=16MB")

            // --- CHECKPOINT & STORAGE (SSD OPTIMIZED) ---
            // Set to 1.1 for NVMe/Good SSDs. Essential for DiskANN performance
            // as it encourages the planner to use index scans over table scans.
            .WithArgs("-c", "random_page_cost=1.1")
            .WithArgs("-c", "effective_io_concurrency=200")

            // Checkpoint target 0.9 spreads I/O load, reducing spikes
            .WithArgs("-c", "checkpoint_completion_target=0.9")
            .WithArgs("-c", "checkpoint_timeout=15min")
            .WithArgs("-c", $"max_wal_size={maxWalSizeGb}GB")
            .WithArgs("-c", "min_wal_size=1GB")

            // --- LOGGING (Optional but recommended for Prod) ---
            // Helps debug slow queries that might exceed work_mem
            .WithArgs("-c", "log_min_duration_statement=1000");
    }

    /// <summary>
    /// https://github.com/dotnet/aspire/issues/11710
    /// </summary>
    public static IResourceBuilder<PostgresServerResource> WithV18DataVolume(
        this IResourceBuilder<PostgresServerResource> builder, string? name = null, bool isReadOnly = false)
    {
        return builder.WithVolume(name ?? VolumeNameGenerator.Generate(builder, "data"),
            "/var/lib/postgresql/18/docker", isReadOnly);
    }
}
