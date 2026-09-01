using System.IO.Compression;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.WebUtilities;

namespace Boilerplate.Server.Api.Features.PersonalData;

/// <summary>
/// Builds the Article 15 / 20 answer: one zip holding a machine-readable <c>data.json</c> and the account's files.
/// Writes to the response rather than returning bytes, so the two callers - the user's own download and an admin
/// answering a request that arrived by e-mail - cannot produce different artefacts.
/// </summary>
public partial class PersonalDataExportService
{
    [AutoInject] private TimeProvider timeProvider = default!;
    [AutoInject] private IEnumerable<IPersonalDataSource> sources = default!;

    /// <summary>
    /// <c>data.json</c> is the "structured, commonly used and machine-readable" half Article 20 asks for; the zip is
    /// its envelope, because base64ing the photograph into the json inflates it by a third.
    /// </summary>
    public async Task WriteTo(Guid userId, HttpResponse response, CancellationToken cancellationToken)
    {
        var exportedOn = timeProvider.GetUtcNow();

        response.ContentType = "application/zip";
        response.Headers.CacheControl = "no-store";
        response.Headers.ContentDisposition = $"attachment; filename=\"personal-data-{userId}-{exportedOn:yyyy-MM-dd}.zip\"";

        // ZipArchive writes synchronously and Kestrel's response body refuses that. FileBufferingWriteStream keeps
        // memory bounded (spilling to a temp file), and nothing reaches the response until the archive is complete -
        // so a source that throws halfway yields an error response rather than a truncated zip.
        await using var buffer = new FileBufferingWriteStream(memoryThreshold: 4 * 1024 * 1024);

        // Scoped: disposing the archive is what writes its central directory, and without that there is no zip to
        // drain. leaveOpen so it does not take the buffer with it.
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            await WriteEntries(userId, archive, exportedOn, cancellationToken);
        }

        await buffer.DrainBufferAsync(response.Body, cancellationToken);
    }

    private async Task WriteEntries(Guid userId, ZipArchive archive, DateTimeOffset exportedOn, CancellationToken cancellationToken)
    {
        var sections = new JsonObject();
        List<(string Path, PersonalDataFile File)> files = [];

        foreach (var source in sources.OrderBy(source => source.Order).ThenBy(source => source.Key))
        {
            var sourceFiles = await source.ExportFiles(userId, cancellationToken);
            var filePaths = new JsonArray();

            foreach (var sourceFile in sourceFiles)
            {
                var path = $"files/{source.Key}/{sourceFile.Name}";
                files.Add((path, sourceFile));
                filePaths.Add(path);
            }

            var recipients = new JsonArray();
            foreach (var recipient in source.Recipients)
            {
                recipients.Add(recipient);
            }

            var section = new JsonObject
            {
                // Beside the data, the things Article 15 asks for that a dump of rows does not answer.
                ["purpose"] = source.Purpose,
                ["retention"] = source.Retention,
                ["recipients"] = recipients,
                ["erasedOnAccountDeletion"] = source.Erasure is not PersonalDataErasure.RetentionJobOnly,
                ["erasurePath"] = source.Erasure.ToString(),
                ["data"] = await source.Export(userId, cancellationToken),
                ["files"] = filePaths
            };

            if (source.Notes is not null)
            {
                section["notes"] = source.Notes;
            }

            sections[source.Key] = section;
        }

        var export = new JsonObject
        {
            ["exportedOn"] = exportedOn,
            ["subjectUserId"] = userId,
            ["notice"] = "Personal data held by this application about the account named in subjectUserId. Credentials (password hash, two-factor secret, push encryption keys) are deliberately excluded: handing them out would weaken the account without telling the reader anything about themselves.",
            ["sections"] = sections
        };

        // First entry, so the reader opening the zip sees the readable half before the blobs.
        var dataEntry = archive.CreateEntry("data.json", CompressionLevel.Optimal);
        await using (var dataStream = dataEntry.Open())
        {
            await using var writer = new Utf8JsonWriter(dataStream, new JsonWriterOptions { Indented = true });
            export.WriteTo(writer);
        }

        foreach (var (path, file) in files)
        {
            // NoCompression: every blob the app stores is already a compressed image format.
            var fileEntry = archive.CreateEntry(path, CompressionLevel.NoCompression);
            await using var fileEntryStream = fileEntry.Open();
            await using var content = await file.OpenRead(cancellationToken);
            await content.CopyToAsync(fileEntryStream, cancellationToken);
        }
    }
}
