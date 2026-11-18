using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

// The row hands its item to the grid's columns, which read its properties by name (BitDataGridPropertyAccessor), so
// it carries the same annotation as BitDataGrid<TItem>: the markup cannot put an attribute on a type parameter.
/// <summary>
/// A row of <see cref="BitDataGrid{TItem}"/>, rendered by the grid alone: it is not meant to be used on its own.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public partial class _BitDataGridRow<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TItem>;
