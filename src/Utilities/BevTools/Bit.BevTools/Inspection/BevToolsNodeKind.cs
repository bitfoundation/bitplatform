namespace Bit.BevTools;

public enum BevToolsNodeKind
{
    Null,
    Primitive,
    String,
    Enum,
    Object,
    Collection,
    Dictionary,
    Redacted,
    Error,
    Special,
    Circular,
}
