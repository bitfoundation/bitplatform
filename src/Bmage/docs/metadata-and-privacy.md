# Metadata and privacy

A photograph from a phone usually carries the location it was taken, the device's serial number, and the exact time. If
your application lets users upload an avatar and you pass their file through untouched, you have published all of that.

Bmage removes metadata by default. You have to ask for it to be kept.

## The three policies

```csharp
new ImageExportOptions { Metadata = MetadataPolicy.Remove }          // the default
new ImageExportOptions { Metadata = MetadataPolicy.StripSensitive }
new ImageExportOptions { Metadata = MetadataPolicy.Preserve }
```

| Policy | Keeps | Removes |
| --- | --- | --- |
| `Remove` | nothing | EXIF, XMP, ICC profile, thumbnail |
| `StripSensitive` | camera, lens, exposure, dates, copyright, description, ICC profile | GPS, serial numbers, owner name, user comments, maker notes, unique IDs, embedded thumbnail, XMP |
| `Preserve` | everything the target format can carry | the embedded thumbnail, which would be stale |

`StripSensitive` drops XMP entirely. XMP can carry location and device identifiers of its own, and there is no reliable
way to filter it field by field, so it is safer to drop it than to pass through something unexamined.

All three policies reset the stored orientation to normal and update the recorded pixel dimensions, because the exported
pixels are already upright and already the new size. Leaving a rotation flag on an image whose pixels have been rotated
is how images end up sideways twice.

## Reading what is there

```csharp
using var image = await loader.LoadAsync(file);
var exif = image.Metadata?.Exif;

if (exif is not null)
{
    Console.WriteLine($"{exif.Make} {exif.Model}");
    Console.WriteLine($"Lens: {exif.LensModel}");
    Console.WriteLine($"{exif.ExposureTime}s at f/{exif.FNumber}, ISO {exif.Iso}");
    Console.WriteLine($"Taken {exif.DateTaken}");

    if (exif.Location is { } location)
        Console.WriteLine($"Location: {location.Latitude}, {location.Longitude}");
}
```

Every tag remains reachable even when there is no typed accessor:

```csharp
var artist = exif.Primary.Get(ExifTag.Artist)?.GetString();
var custom = exif.Exif.Get(0x9C9B)?.GetString();
```

## Warning users

The demo's headless page shows the pattern: read the location, tell the user it is there, and say what will happen to it.

```razor
@if (exif?.Location is { } location)
{
    <p class="warning">
        This photo records where it was taken (@location.ToString()).
        It will be removed when you save.
    </p>
}
```

## Writing metadata

```csharp
var exif = new ExifData()
    .WithPrimary(new ExifDirectory()
        .With(ExifTag.Artist, ExifValue.FromString("Sam Rivera"))
        .With(ExifTag.Copyright, ExifValue.FromString("© 2026 Sam Rivera")));

image.Metadata = new ImageMetadata(exif);

var encoded = await exporter.EncodeAsync(image, new ImageExportOptions
{
    Format = ImageFormat.Jpeg,
    Metadata = MetadataPolicy.Preserve,
});
```

`ExifValue.FromString` writes UTF-8. The EXIF specification calls the type ASCII, but cameras and editors have written
UTF-8 in these fields for years, and restricting to ASCII would silently turn "©" into "?" and mangle any name with an
accent. There is a test asserting that a copyright symbol survives a round trip, and it caught exactly that bug during
development.

## Orientation

Phones do not rotate pixels when you turn the device; they record a rotation flag. An image that looks upright in a photo
viewer will appear sideways on a canvas unless that flag is applied.

Bmage applies it during decoding by default, so you rarely think about it:

```csharp
await loader.LoadAsync(file);                                          // upright
await loader.LoadAsync(file, new DecodeOptions { AutoOrient = false }); // as stored
```

With `AutoOrient = false` the orientation is reported rather than applied, so you can decide:

```csharp
var orientation = image.Metadata!.Orientation;
if (orientation.SwapsDimensions()) { /* portrait photo stored as landscape */ }
```

The eight EXIF orientations form the dihedral group of the square, and `OrientationMath` implements the algebra:
`Then` composes two, `Inverse` undoes one, `SwapsDimensions` tells you whether width and height exchange, and `ToMatrix`
gives the coordinate mapping. A test verifies all 64 compositions against actually transforming pixels, and another
verifies the matrix against the pixel mapping, because getting transpose and transverse the wrong way round is easy and
invisible until someone uploads an unusual photo.

## What each format can carry

| Format | EXIF | ICC | XMP |
| --- | :---: | :---: | :---: |
| JPEG | yes | yes | yes |
| PNG | yes (`eXIf`) | yes (`iCCP`) | yes (`iTXt`) |
| WebP | yes | yes | yes |
| AVIF | not written by this library | no | no |
| BMP, GIF | no | no | no |

Metadata is attached after encoding, by rewriting the container. The browser's encoders discard metadata, so it is
re-inserted in C# according to your policy. If a container rewrite fails, the correctly encoded image is returned without
metadata rather than the whole export failing.

## Malformed metadata

EXIF parsing is defensive throughout: bad offsets, circular IFD references, absurd counts and truncated data are ignored
rather than thrown. A corrupt EXIF block must never stop an image from loading, and there are tests that truncate valid
data at forty different lengths and corrupt offsets to confirm nothing escapes.

## A checklist for user uploads

1. Leave the default `MetadataPolicy.Remove` unless you have a reason.
2. If you keep metadata for a gallery, use `StripSensitive`, not `Preserve`.
3. Tell users what is in their file before they publish it.
4. Leave `AutoOrient` on so the exported pixels match what the user saw.
5. Remember that redaction is about pixels, and metadata is a separate leak.
