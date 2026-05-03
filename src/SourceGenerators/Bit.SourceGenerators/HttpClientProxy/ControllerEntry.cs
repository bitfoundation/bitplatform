namespace Bit.SourceGenerators;

/// <summary>
/// Flat, structurally equatable representation of one IAppController interface captured during
/// the incremental generator transform phase. All Roslyn symbol data is serialised to strings
/// so that the record struct has correct value-based equality for incremental caching.
///
/// Encoding separators (ASCII control characters — never appear in C# identifiers or type names):
///   \x1E  RS  –  between action records
///   \x1F  US  –  between fields inside one action record
///   \x1D  GS  –  between parameters inside one action record
///   \x1C  FS  –  between sub-fields inside one parameter entry
/// </summary>
internal readonly record struct ControllerEntry(
    string SymbolDisplay,       // e.g. "IMyController"
    string SymbolDisplayNoNull, // same without nullable annotation
    string ClassName,           // sanitized full type path, e.g. "MyApp_Controllers_IUsersController" (generated proxy class name)
    string EncodedActions);     // all action data encoded with the separators above
