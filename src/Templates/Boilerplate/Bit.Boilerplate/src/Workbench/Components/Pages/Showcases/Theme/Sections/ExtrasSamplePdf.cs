using System.Text;

namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme.Sections;

/// <summary>
/// The document the BitPdfViewer sample shows: a one-page invoice for the first order of the data grid, built in memory
/// as a plain PDF in Helvetica, black on white, so nothing has to be downloaded.
/// </summary>
public static class ExtrasSamplePdf
{
    /// <summary>The PDF file's bytes, built once.</summary>
    public static byte[] Bytes { get; } = Build();

    /// <summary>
    /// Writes the five objects of a one-page PDF, then the cross-reference table that points at each one by its byte
    /// offset. Everything is ASCII, so a character is a byte and the offsets are the builder's lengths.
    /// </summary>
    private static byte[] Build()
    {
        // The page's drawing: lines of text placed from the bottom-left corner, in points, and two rules.
        string[] drawing =
        [
            "BT /F2 18 Tf 32 250 Td (Invoice 8042) Tj ET",
            "BT /F1 10 Tf 32 233 Td (Amsterdam, Jordaan  -  18 May 2026  -  Emma de Vries) Tj ET",
            "0.6 w 32 220 m 388 220 l S",
            "BT /F1 11 Tf 32 200 Td (Flat white  x 2) Tj ET",
            "BT /F1 11 Tf 336 200 Td (EUR 7.00) Tj ET",
            "BT /F1 11 Tf 32 182 Td (Butter croissant  x 1) Tj ET",
            "BT /F1 11 Tf 336 182 Td (EUR 3.25) Tj ET",
            "BT /F1 11 Tf 32 164 Td (Stroopwafel  x 1) Tj ET",
            "BT /F1 11 Tf 336 164 Td (EUR 2.25) Tj ET",
            "32 150 m 388 150 l S",
            "BT /F2 12 Tf 32 130 Td (Total) Tj ET",
            "BT /F2 12 Tf 330 130 Td (EUR 12.50) Tj ET",
            "BT /F1 9 Tf 32 40 Td (Paid by card. Thank you for your visit!) Tj ET"
        ];

        var content = string.Join('\n', drawing);

        string[] objects =
        [
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 420 297] /Resources << /Font << /F1 4 0 R /F2 5 0 R >> >> /Contents 6 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>",
            $"<< /Length {content.Length} >>\nstream\n{content}\nendstream"
        ];

        var pdf = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int>();

        for (var index = 0; index < objects.Length; index++)
        {
            offsets.Add(pdf.Length);
            pdf.Append(CultureInfo.InvariantCulture, $"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
        }

        var crossReference = pdf.Length;

        // Each entry of the table is exactly 20 bytes, its line break included.
        pdf.Append(CultureInfo.InvariantCulture, $"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");

        foreach (var offset in offsets)
        {
            pdf.Append(CultureInfo.InvariantCulture, $"{offset:D10} 00000 n \n");
        }

        pdf.Append(CultureInfo.InvariantCulture, $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{crossReference}\n%%EOF\n");

        return Encoding.ASCII.GetBytes(pdf.ToString());
    }
}
