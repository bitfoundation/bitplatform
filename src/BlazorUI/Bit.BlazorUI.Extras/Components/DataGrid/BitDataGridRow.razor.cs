using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

// The row hands its item to the grid's columns, which read its properties by name (BitDataGridPropertyAccessor), so
// it carries the same annotation as BitDataGrid<TItem>: the markup cannot put an attribute on a type parameter.
public partial class BitDataGridRow<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TItem>;
