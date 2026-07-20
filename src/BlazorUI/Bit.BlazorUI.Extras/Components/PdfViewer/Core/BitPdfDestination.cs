// Explicit-destination parsing and normalization.
// A destination array is [pageRef /Type args...]; see PDF 32000-1:2008 §12.3.2.

namespace Bit.BlazorUI;

/// <summary>
/// A resolved explicit destination: the target page plus the view parameters
/// (fit mode, position, zoom). Unspecified numeric parameters are <c>null</c>.
/// </summary>
public sealed class BitPdfDestination
{
    /// <summary>The target page (1-based), or <c>null</c> if it could not be resolved.</summary>
    public int? PageNumber { get; init; }

    /// <summary>The requested view-fit mode.</summary>
    public BitPdfDestinationFit Fit { get; init; } = BitPdfDestinationFit.Unknown;

    /// <summary>Left edge / X coordinate (XYZ, FitV, FitR, FitBV), when specified.</summary>
    public double? Left { get; init; }

    /// <summary>Top edge / Y coordinate (XYZ, FitH, FitR, FitBH), when specified.</summary>
    public double? Top { get; init; }

    /// <summary>Right edge (FitR), when specified.</summary>
    public double? Right { get; init; }

    /// <summary>Bottom edge (FitR), when specified.</summary>
    public double? Bottom { get; init; }

    /// <summary>Zoom factor (XYZ); <c>0</c>/<c>null</c> means "retain current zoom".</summary>
    public double? Zoom { get; init; }

    /// <summary>Builds a destination from the tail of a destination array (the fit name and its args).</summary>
    internal static BitPdfDestination FromArray(int? pageNumber, List<object?> arr, IBitPdfXRef xref)
    {
        // arr[0] is the page target; arr[1] is the fit name; the rest are args.
        string fitName = arr.Count > 1 && xref.FetchIfRef(arr[1]) is BitPdfName n ? n.Value : "";

        double? Arg(int index)
        {
            if (index >= arr.Count)
            {
                return null;
            }
            object? v = xref.FetchIfRef(arr[index]);
            return v is double d ? d : null; // PDF null => "unchanged"
        }

        return fitName switch
        {
            "XYZ" => new BitPdfDestination
            {
                PageNumber = pageNumber,
                Fit = BitPdfDestinationFit.XYZ,
                Left = Arg(2),
                Top = Arg(3),
                Zoom = Arg(4),
            },
            "Fit" => new BitPdfDestination { PageNumber = pageNumber, Fit = BitPdfDestinationFit.Fit },
            "FitB" => new BitPdfDestination { PageNumber = pageNumber, Fit = BitPdfDestinationFit.FitB },
            "FitH" => new BitPdfDestination { PageNumber = pageNumber, Fit = BitPdfDestinationFit.FitH, Top = Arg(2) },
            "FitBH" => new BitPdfDestination { PageNumber = pageNumber, Fit = BitPdfDestinationFit.FitBH, Top = Arg(2) },
            "FitV" => new BitPdfDestination { PageNumber = pageNumber, Fit = BitPdfDestinationFit.FitV, Left = Arg(2) },
            "FitBV" => new BitPdfDestination { PageNumber = pageNumber, Fit = BitPdfDestinationFit.FitBV, Left = Arg(2) },
            "FitR" => new BitPdfDestination
            {
                PageNumber = pageNumber,
                Fit = BitPdfDestinationFit.FitR,
                Left = Arg(2),
                Bottom = Arg(3),
                Right = Arg(4),
                Top = Arg(5),
            },
            _ => new BitPdfDestination { PageNumber = pageNumber, Fit = BitPdfDestinationFit.Unknown },
        };
    }
}
