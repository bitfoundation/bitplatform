namespace Bit.Blow;

/// <summary>Settings for <see cref="BlowLayout.Grid"/>.</summary>
public sealed class BlowGridLayoutOptions : BlowLayoutOptions
{
    /// <inheritdoc/>
    public override void Arrange(IReadOnlyList<BlowNode> nodes, IReadOnlyList<BlowEdge> edges) => BlowLayout.Grid(nodes, this);

    /// <summary>
    /// Number of columns. When null a roughly square grid is used, which is what "just tidy
    /// these up" usually means.
    /// </summary>
    public int? Columns { get; set; }

    /// <summary>Horizontal gap between cells, in flow units.</summary>
    public double ColumnGap { get; set; } = 40;

    /// <summary>Vertical gap between cells, in flow units.</summary>
    public double RowGap { get; set; } = 40;
}
