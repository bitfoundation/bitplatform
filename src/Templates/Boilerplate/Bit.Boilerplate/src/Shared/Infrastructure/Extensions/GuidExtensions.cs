//+:cnd:noEmit
namespace System;

public static class GuidExtensions
{
    extension(Guid source)
    {
        /// <summary>
        /// Generates a sequential GUID optimized for database primary key usage.
        /// 
        /// <para>
        /// <b>Warning</b>
        /// Both PostgreSQL and SQL Server are configured to natively generate sequential identifiers 
        /// if the entity's ID is not provided. Use this method only when application-side GUID key generation is required.
        /// </para>
        /// 
        /// <para>
        /// <b>Benefit of Sequential GUIDs as Clustered Indexes</b>
        /// Even with NVMe SSDs, random GUIDs can degrade performance due to frequent Page Splits; 
        /// therefore, sequential identifiers are preferred.
        /// </para>
        /// 
        /// <para>
        /// <b>Guid.CreateVersion7() vs CreateSequentialGuid()</b>
        /// While <see cref="Guid.CreateVersion7()"/> 
        /// provides time-ordered GUIDs for most engines, SQL Server employs a unique sorting logic 
        /// that necessitates byte reordering to maintain physical sequentiality.
        /// The <see cref="CreateSequentialGuid()"/> method implements this SQL Server-specific byte rearrangement.
        /// </para>
        /// 
        /// <para>
        /// <b>Handling Offline/Sync Scenarios</b>
        /// In offline-capable applications, using application-side GUIDs is useful when inserting parent and child records before syncing them to the server. 
        /// But regardless of sequentiality, "Late-Arriving Data" (records arriving out of chronological order relative to the server) will inevitably cause index fragmentation.
        /// For high-scale offline scenarios, it is recommended to use a server-generated <c>ServerCreatedAtUtc</c> 
        /// column as the Clustered Index and move the GUID <c>Id</c> and <c>IsArchived</c> to a Non-Clustered filtered index.
        /// </para>
        /// </summary>
        public static Guid CreateSequentialGuid()
        {
            Guid standardV7 = Guid.CreateVersion7();

            //#if (database != "SqlServer")
            return standardV7;
            // SQL Server specific byte rearrangement is not needed for your chosen database engine.
            //#else

            Span<byte> bytes = stackalloc byte[16];
            standardV7.TryWriteBytes(bytes, bigEndian: true, out _);

            // Version 7 structure (big-endian):
            // bytes[0-5]: 48-bit Unix timestamp in milliseconds
            // bytes[6-7]: 4-bit version (0111) + 12-bit random
            // bytes[8-15]: 2-bit variant (10) + 62-bit random

            Span<byte> sqlBytes = stackalloc byte[16];

            /* SQL SERVER SORTING PRIORITY:
               1st: Bytes 10-15 (Must contain the most significant Time bits)
               2nd: Bytes 8-9
               3rd: Bytes 6-7
               4th-6th: Bytes 0-5 (Least significant)
            */

            // Move the 48-bit Timestamp (6 bytes) to the very end (10-15)
            // This makes the timestamp the PRIMARY sorting criteria for SQL Server
            bytes[..6].CopyTo(sqlBytes.Slice(10, 6));

            // Move Version, Variant, and Random bytes to the beginning (0-9)
            // These will only be compared if the timestamp is identical
            bytes.Slice(6, 10).CopyTo(sqlBytes[..10]);

            // Use bigEndian: true because we manually laid out the bytes 
            // for SQL Server's internal storage format.
            return new Guid(sqlBytes, bigEndian: true);
            //#endif
        }
    }
}
