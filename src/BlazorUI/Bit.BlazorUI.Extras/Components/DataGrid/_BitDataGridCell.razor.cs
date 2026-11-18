using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

// The cell reads its item through its column's property accessor, so it carries the same annotation as
// BitDataGrid<TItem>: the markup cannot put an attribute on a type parameter.
/// <summary>
/// A cell of <see cref="BitDataGrid{TItem}"/>, rendered by the grid alone: it is not meant to be used on its own.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public partial class _BitDataGridCell<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TItem>;
