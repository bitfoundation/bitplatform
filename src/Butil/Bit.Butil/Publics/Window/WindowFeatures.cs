using System.Linq;

namespace Bit.Butil;

public class WindowFeatures
{
    public bool Popup { get; set; }
    public uint Width { get; set; }
    public uint Height { get; set; }
    public int Left { get; set; } = -1;
    public int Top { get; set; } = -1;
    public bool NoOpener { get; set; }
    public bool NoReferrer { get; set; }

    public override string ToString()
    {
        var list = new[] {
            Popup ? "popup=true" : null,
            Width >= 100 ? $"width={Width}" : null,
            Height >= 100 ? $"height={Height}" : null,
            Left > -1 ? $"left={Left}" : null,
            Top > -1 ? $"top={Top}" : null,
            NoOpener ? "noopener=true" : null,
            NoReferrer ? "noreferrer=true" : null,
        };
        return string.Join(',', list.Where(i => i is not null));
    }
}
