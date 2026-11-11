namespace Bit.BlazorUI.Tests.Trimming.Infrastructure;

/// <summary>
/// A bundle as the trimming put it back together for one publish, read off the line it logs for it
/// (TrimBitBlazorUIAssets.Report): how many of the package's pieces it kept, and the bytes it came to out of the full
/// bundle's.
/// </summary>
/// <param name="Package">The package the bundle is a static web asset of ("Bit.BlazorUI").</param>
/// <param name="Bundle">The bundle's path under the package's static web assets ("scripts/bit.blazorui.js").</param>
internal sealed record TrimmedBundle(string Package, string Bundle, int KeptPieces, int AllPieces, long TrimmedBytes, long FullBytes);
