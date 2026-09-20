using System;

namespace Bit.Butil.Build;

/// <summary>
/// The metadata table stream (<c>#~</c>) of a managed assembly, read far enough to answer the two questions
/// the script trimming asks of an assembly that ILLink never touched: which <c>Bit.Butil</c> types some
/// consumer assembly names (its <c>TypeRef</c> rows), and which method bodies belong to which type inside
/// Bit.Butil itself (its <c>TypeDef</c> and <c>MethodDef</c> rows).
/// </summary>
/// <remarks>
/// Deliberately partial. Reaching a table means knowing the row size of every table before it, so a reader
/// that wanted <c>AssemblyRef</c> (0x23) would have to carry the column layout of all thirty-odd tables in
/// between - a lot of code whose only use here would be to say which assembly a <c>TypeRef</c> resolves
/// against. Matching a <c>TypeRef</c> on its namespace and name instead costs nothing and needs only the
/// tables up to <c>MemberRef</c> (0x0A), which is where this stops. See ECMA-335 II.22 and II.24.2.6.
/// <br/>
/// One consequence of stopping there: a <c>MemberRef</c> whose parent is a <c>TypeSpec</c> - a call on a
/// generic instantiation - cannot be resolved back to the type it instantiates, because that means reading
/// a signature blob out of a table thirty rows further on. Such a call is only ever a second path to a type
/// the same method already names some other way, and the module map it feeds is checked against what ILLink
/// itself concludes (see the repository's trimming harness), so the gap is a measured one rather than an
/// assumed one.
/// </remarks>
public sealed class MetadataTables
{
    private const int ModuleTable = 0x00;
    private const int TypeRefTable = 0x01;
    private const int TypeDefTable = 0x02;
    private const int FieldPtrTable = 0x03;
    private const int FieldTable = 0x04;
    private const int MethodPtrTable = 0x05;
    private const int MethodDefTable = 0x06;
    private const int ParamPtrTable = 0x07;
    private const int ParamTable = 0x08;
    private const int InterfaceImplTable = 0x09;
    private const int MemberRefTable = 0x0A;
    private const int ConstantTable = 0x0B;
    private const int CustomAttributeTable = 0x0C;
    private const int FieldMarshalTable = 0x0D;
    private const int DeclSecurityTable = 0x0E;
    private const int ClassLayoutTable = 0x0F;
    private const int FieldLayoutTable = 0x10;
    private const int StandAloneSigTable = 0x11;
    private const int EventMapTable = 0x12;
    private const int EventPtrTable = 0x13;
    private const int EventTable = 0x14;
    private const int PropertyMapTable = 0x15;
    private const int PropertyPtrTable = 0x16;
    private const int PropertyTable = 0x17;
    private const int MethodSemanticsTable = 0x18;
    private const int MethodImplTable = 0x19;
    private const int ModuleRefTable = 0x1A;
    private const int TypeSpecTable = 0x1B;
    private const int ImplMapTable = 0x1C;
    private const int FieldRvaTable = 0x1D;
    private const int EncLogTable = 0x1E;
    private const int EncMapTable = 0x1F;
    private const int AssemblyTable = 0x20;
    private const int AssemblyProcessorTable = 0x21;
    private const int AssemblyOsTable = 0x22;
    private const int AssemblyRefTable = 0x23;
    private const int AssemblyRefProcessorTable = 0x24;
    private const int AssemblyRefOsTable = 0x25;
    private const int FileTable = 0x26;
    private const int ExportedTypeTable = 0x27;
    private const int ManifestResourceTable = 0x28;
    private const int NestedClassTable = 0x29;
    private const int GenericParamTable = 0x2A;
    private const int MethodSpecTable = 0x2B;
    private const int GenericParamConstraintTable = 0x2C;

    /// <summary>The last table this reader can lay out; nothing defines one past it.</summary>
    private const int LastTable = GenericParamConstraintTable;

    private readonly PeImage _image;

    private readonly int[] _rows = new int[64];

    private readonly int[] _rowSize = new int[64];

    private readonly int[] _tableOffset = new int[64];

    private readonly int _stringIndexSize;

    private readonly int _guidIndexSize;

    private readonly int _blobIndexSize;

    private MetadataTables(PeImage image)
    {
        _image = image;

        var stream = image.Tables;
        if (stream.IsEmpty) throw image.Invalid("no metadata table stream");

        // Header: reserved (4), major/minor version (1 each), heap sizes (1), reserved (1), the 64-bit
        // present-table mask, the 64-bit sorted mask, then one row count per present table.
        var heapSizes = image.ReadByte(stream.Offset + 6);
        _stringIndexSize = (heapSizes & 0x01) != 0 ? 4 : 2;
        _guidIndexSize = (heapSizes & 0x02) != 0 ? 4 : 2;
        _blobIndexSize = (heapSizes & 0x04) != 0 ? 4 : 2;

        var valid = image.ReadUInt32(stream.Offset + 8) | ((ulong)image.ReadUInt32(stream.Offset + 12) << 32);

        var position = stream.Offset + 24;
        for (var table = 0; table < 64; table++)
        {
            if ((valid & (1UL << table)) == 0) continue;

            _rows[table] = image.ReadInt32(position);
            position += 4;
        }

        // An "extra data" word only the edit-and-continue stream shape carries, between the row counts and
        // the rows themselves. Skipping it keeps every row offset below right for those assemblies too.
        if ((heapSizes & 0x40) != 0) position += 4;

        // Row sizes, and from them the offset of each table: the tables follow one another in index order, so
        // a table's offset is where every earlier table's rows end. Every table therefore has to be sized,
        // including the ones nothing here reads - a single wrong width and every table after it is read at
        // the wrong offset, which is why the whole of ECMA-335 II.22 is written out rather than the four
        // tables this actually looks at.
        _rowSize[ModuleTable] = 2 + _stringIndexSize + (3 * _guidIndexSize);
        _rowSize[TypeRefTable] = CodedIndexSize(2, ModuleTable, ModuleRefTable, AssemblyRefTable, TypeRefTable) + (2 * _stringIndexSize);
        _rowSize[TypeDefTable] = 4 + (2 * _stringIndexSize) + CodedIndexSize(2, TypeDefTable, TypeRefTable, TypeSpecTable) + TableIndexSize(FieldTable) + TableIndexSize(MethodDefTable);
        _rowSize[FieldPtrTable] = TableIndexSize(FieldTable);
        _rowSize[FieldTable] = 2 + _stringIndexSize + _blobIndexSize;
        _rowSize[MethodPtrTable] = TableIndexSize(MethodDefTable);
        _rowSize[MethodDefTable] = 4 + 2 + 2 + _stringIndexSize + _blobIndexSize + TableIndexSize(ParamTable);
        _rowSize[ParamPtrTable] = TableIndexSize(ParamTable);
        _rowSize[ParamTable] = 2 + 2 + _stringIndexSize;
        _rowSize[InterfaceImplTable] = TableIndexSize(TypeDefTable) + CodedIndexSize(2, TypeDefTable, TypeRefTable, TypeSpecTable);
        _rowSize[MemberRefTable] = MemberRefParentSize + _stringIndexSize + _blobIndexSize;
        _rowSize[ConstantTable] = 2 + CodedIndexSize(2, FieldTable, ParamTable, PropertyTable) + _blobIndexSize;
        _rowSize[CustomAttributeTable] = HasCustomAttributeSize + CodedIndexSize(3, MethodDefTable, MemberRefTable) + _blobIndexSize;
        _rowSize[FieldMarshalTable] = CodedIndexSize(1, FieldTable, ParamTable) + _blobIndexSize;
        _rowSize[DeclSecurityTable] = 2 + CodedIndexSize(2, TypeDefTable, MethodDefTable, AssemblyTable) + _blobIndexSize;
        _rowSize[ClassLayoutTable] = 2 + 4 + TableIndexSize(TypeDefTable);
        _rowSize[FieldLayoutTable] = 4 + TableIndexSize(FieldTable);
        _rowSize[StandAloneSigTable] = _blobIndexSize;
        _rowSize[EventMapTable] = TableIndexSize(TypeDefTable) + TableIndexSize(EventTable);
        _rowSize[EventPtrTable] = TableIndexSize(EventTable);
        _rowSize[EventTable] = 2 + _stringIndexSize + CodedIndexSize(2, TypeDefTable, TypeRefTable, TypeSpecTable);
        _rowSize[PropertyMapTable] = TableIndexSize(TypeDefTable) + TableIndexSize(PropertyTable);
        _rowSize[PropertyPtrTable] = TableIndexSize(PropertyTable);
        _rowSize[PropertyTable] = 2 + _stringIndexSize + _blobIndexSize;
        _rowSize[MethodSemanticsTable] = 2 + TableIndexSize(MethodDefTable) + CodedIndexSize(1, EventTable, PropertyTable);
        _rowSize[MethodImplTable] = TableIndexSize(TypeDefTable) + (2 * MethodDefOrRefSize);
        _rowSize[ModuleRefTable] = _stringIndexSize;
        _rowSize[TypeSpecTable] = _blobIndexSize;
        _rowSize[ImplMapTable] = 2 + CodedIndexSize(1, FieldTable, MethodDefTable) + _stringIndexSize + TableIndexSize(ModuleRefTable);
        _rowSize[FieldRvaTable] = 4 + TableIndexSize(FieldTable);
        _rowSize[EncLogTable] = 4 + 4;
        _rowSize[EncMapTable] = 4;
        _rowSize[AssemblyTable] = 4 + (4 * 2) + 4 + _blobIndexSize + (2 * _stringIndexSize);
        _rowSize[AssemblyProcessorTable] = 4;
        _rowSize[AssemblyOsTable] = 4 + 4 + 4;
        _rowSize[AssemblyRefTable] = (4 * 2) + 4 + _blobIndexSize + (2 * _stringIndexSize) + _blobIndexSize;
        _rowSize[AssemblyRefProcessorTable] = 4 + TableIndexSize(AssemblyRefTable);
        _rowSize[AssemblyRefOsTable] = 4 + 4 + 4 + TableIndexSize(AssemblyRefTable);
        _rowSize[FileTable] = 4 + _stringIndexSize + _blobIndexSize;
        _rowSize[ExportedTypeTable] = 4 + 4 + (2 * _stringIndexSize) + ImplementationSize;
        _rowSize[ManifestResourceTable] = 4 + 4 + _stringIndexSize + ImplementationSize;
        _rowSize[NestedClassTable] = 2 * TableIndexSize(TypeDefTable);
        _rowSize[GenericParamTable] = 2 + 2 + CodedIndexSize(1, TypeDefTable, MethodDefTable) + _stringIndexSize;
        _rowSize[MethodSpecTable] = MethodDefOrRefSize + _blobIndexSize;
        _rowSize[GenericParamConstraintTable] = TableIndexSize(GenericParamTable) + CodedIndexSize(2, TypeDefTable, TypeRefTable, TypeSpecTable);

        for (var table = ModuleTable; table <= LastTable; table++)
        {
            _tableOffset[table] = position;
            position += _rows[table] * _rowSize[table];
        }

        // A table past the ones laid out above means an assembly this reader cannot walk. Saying so beats
        // reading the tables it does know at offsets that are quietly wrong.
        for (var table = LastTable + 1; table < 64; table++)
        {
            if (_rows[table] != 0) throw image.Invalid($"a metadata table (0x{table:X2}) this reader does not know the shape of");
        }

        if (position > stream.Offset + stream.Size) throw image.Invalid("metadata tables that run past the end of their stream");
    }

    /// <summary>Reads an image's table stream. Never null; an assembly always has one.</summary>
    public static MetadataTables Read(PeImage image) => new(image);

    // The coded indexes used by more than one table's layout, named once so the widths cannot drift apart.
    private int MemberRefParentSize => CodedIndexSize(3, TypeDefTable, TypeRefTable, ModuleRefTable, MethodDefTable, TypeSpecTable);

    private int MethodDefOrRefSize => CodedIndexSize(1, MethodDefTable, MemberRefTable);

    private int ImplementationSize => CodedIndexSize(2, FileTable, AssemblyRefTable, ExportedTypeTable);

    private int HasCustomAttributeSize => CodedIndexSize(5,
        MethodDefTable, FieldTable, TypeRefTable, TypeDefTable, ParamTable, InterfaceImplTable, MemberRefTable,
        ModuleTable, DeclSecurityTable, PropertyTable, EventTable, StandAloneSigTable, ModuleRefTable,
        TypeSpecTable, AssemblyTable, AssemblyRefTable, FileTable, ExportedTypeTable, ManifestResourceTable,
        GenericParamTable, GenericParamConstraintTable, MethodSpecTable);

    /// <summary>How many rows a table has. Tables the assembly does not carry have none.</summary>
    public int RowCount(int table) => _rows[table];

    public int TypeRefCount => _rows[TypeRefTable];

    public int TypeDefCount => _rows[TypeDefTable];

    public int MethodDefCount => _rows[MethodDefTable];

    /// <summary>
    /// One <c>TypeRef</c> row: a type this assembly names but does not define. The resolution scope is
    /// skipped over rather than resolved - see the remarks on the class.
    /// </summary>
    public TypeName TypeRef(int row)
    {
        var position = RowStart(TypeRefTable, row) + CodedIndexSize(2, ModuleTable, ModuleRefTable, AssemblyRefTable, TypeRefTable);

        return new TypeName(ReadString(position + _stringIndexSize), ReadString(position));
    }

    public int MemberRefCount => _rows[MemberRefTable];

    /// <summary>One <c>TypeDef</c> row: a type this assembly defines, and where its members start.</summary>
    public TypeDefinition TypeDef(int row)
    {
        var position = RowStart(TypeDefTable, row);
        var name = ReadString(position + 4);
        var space = ReadString(position + 4 + _stringIndexSize);

        var extendsPosition = position + 4 + (2 * _stringIndexSize);
        var extends = ReadCodedIndex(extendsPosition, 2, TypeDefTable, TypeRefTable, TypeSpecTable);

        var fieldListPosition = extendsPosition + CodedIndexSize(2, TypeDefTable, TypeRefTable, TypeSpecTable);
        var methodListPosition = fieldListPosition + TableIndexSize(FieldTable);

        return new TypeDefinition(row, new TypeName(space, name), extends,
            ReadTableIndex(fieldListPosition, FieldTable), ReadTableIndex(methodListPosition, MethodDefTable));
    }

    /// <summary>
    /// The row range of a type's methods: from its own <c>MethodList</c> up to the next type's, one-based and
    /// end-exclusive, which is how ECMA-335 encodes a member list.
    /// </summary>
    public (int First, int End) MethodRange(int typeRow) => MemberRange(typeRow, MethodDefTable);

    /// <summary>The row range of a type's fields, on the same one-based, end-exclusive scheme.</summary>
    public (int First, int End) FieldRange(int typeRow) => MemberRange(typeRow, FieldTable);

    private (int First, int End) MemberRange(int typeRow, int memberTable)
    {
        var definition = TypeDef(typeRow);
        var first = memberTable == FieldTable ? definition.FieldListStart : definition.MethodListStart;
        var end = _rows[memberTable] + 1;

        if (typeRow < _rows[TypeDefTable])
        {
            var next = TypeDef(typeRow + 1);
            end = memberTable == FieldTable ? next.FieldListStart : next.MethodListStart;
        }

        if (first < 1) first = 1;
        if (end > _rows[memberTable] + 1) end = _rows[memberTable] + 1;

        return (first, Math.Max(first, end));
    }

    /// <summary>The RVA of a method's IL body. Zero for an abstract, extern or runtime-implemented method.</summary>
    public uint MethodRva(int row) => _image.ReadUInt32(RowStart(MethodDefTable, row));

    /// <summary>
    /// The type a <c>MemberRef</c> is declared on, when that is a type this assembly defines. Nil for the
    /// usual case of a member on another assembly's type, and for the generic instantiations this reader
    /// deliberately does not resolve (see the remarks on the class).
    /// </summary>
    public MetadataToken MemberRefParent(int row)
        => ReadCodedIndex(RowStart(MemberRefTable, row), 3, TypeDefTable, TypeRefTable, ModuleRefTable, MethodDefTable, TypeSpecTable);

    public int MethodSpecCount => _rows[MethodSpecTable];

    /// <summary>
    /// The method a <c>MethodSpec</c> instantiates. A call to a generic method goes through one of these
    /// rather than naming the method directly, so without this a generic call is a reference the walk cannot
    /// see - and inside this library those are the calls that reach the internal interop classes.
    /// </summary>
    public MetadataToken MethodSpecMethod(int row)
        => ReadCodedIndex(RowStart(MethodSpecTable, row), 1, MethodDefTable, MemberRefTable);

    public int NestedClassCount => _rows[NestedClassTable];

    /// <summary>
    /// One <c>NestedClass</c> row as (nested, enclosing) <c>TypeDef</c> rows. The compiler puts an async
    /// method's state machine, an iterator's, and a lambda's closure in a type nested inside the one whose
    /// source they came from - and those bodies are where the calls really live, so a walk that stops at the
    /// enclosing type sees a method that does nothing.
    /// </summary>
    public (int Nested, int Enclosing) NestedClass(int row)
    {
        var position = RowStart(NestedClassTable, row);

        return (ReadTableIndex(position, TypeDefTable), ReadTableIndex(position + TableIndexSize(TypeDefTable), TypeDefTable));
    }

    private int RowStart(int table, int row)
    {
        if (row < 1 || row > _rows[table]) throw _image.Invalid($"a row index outside metadata table 0x{table:X2}");

        return _tableOffset[table] + ((row - 1) * _rowSize[table]);
    }

    private string ReadString(int position)
        => _image.ReadHeapString(_stringIndexSize == 2 ? _image.ReadUInt16(position) : _image.ReadInt32(position));

    private int TableIndexSize(int table) => _rows[table] < 65536 ? 2 : 4;

    private int ReadTableIndex(int position, int table)
        => TableIndexSize(table) == 2 ? _image.ReadUInt16(position) : _image.ReadInt32(position);

    /// <summary>
    /// A coded index is a row number with the table it points into packed into its low bits, so its width is
    /// decided by the largest of the tables it can reach (ECMA-335 II.24.2.6).
    /// </summary>
    private int CodedIndexSize(int tagBits, params int[] tables)
    {
        var largest = 0;
        foreach (var table in tables) largest = Math.Max(largest, _rows[table]);

        return largest < 1 << (16 - tagBits) ? 2 : 4;
    }

    private MetadataToken ReadCodedIndex(int position, int tagBits, params int[] tables)
    {
        var raw = CodedIndexSize(tagBits, tables) == 2 ? _image.ReadUInt16(position) : (uint)_image.ReadInt32(position);
        var tag = (int)(raw & ((1u << tagBits) - 1));

        return tag >= tables.Length
            ? default
            : new MetadataToken(tables[tag], (int)(raw >> tagBits));
    }
}
