using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

// The cell reads its item through its column's property accessor, so it carries the same annotation as
// BitDataGrid<TItem>: the markup cannot put an attribute on a type parameter.
public partial class BitDataGridCellEditor<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TItem>;
