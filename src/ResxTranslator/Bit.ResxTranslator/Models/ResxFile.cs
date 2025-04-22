using System.Globalization;

namespace Bit.ResxTranslator.Models;

public class ResxFile
{
    public required string Language { get; init; }

    public required CultureInfo CultureInfo { get; init; }

    public required string Path { get; init; }

    public ResxFile[] RelatedResxFiles { get; set; } = [];

    public override string ToString()
    {
        return Language;
    }
}
