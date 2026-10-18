// System.Threading.Lock only exists from .NET 9 onwards. On net8.0 the name is aliased to the
// classic monitor object: `new()` and every `lock (...)` site compile unchanged, they just take
// the Monitor path instead of the newer type's fast path.
#if !NET9_0_OR_GREATER
global using Lock = System.Object;
#endif
