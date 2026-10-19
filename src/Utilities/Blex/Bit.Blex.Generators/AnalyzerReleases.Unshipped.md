; Unshipped analyzer releases; see https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
BLEX001 | Bit.Blex.Generators | Error | Store class must be partial
BLEX002 | Bit.Blex.Generators | Error | Cannot derive action name
BLEX003 | Bit.Blex.Generators | Error | Cannot derive computed name
BLEX004 | Bit.Blex.Generators | Error | Computed method must be parameterless
BLEX005 | Bit.Blex.Generators | Error | Generated member name collides
BLEX006 | Bit.Blex.Generators | Error | Unsupported store class shape
BLEX007 | Bit.Blex.Generators | Error | Effect method must return Task or ValueTask
BLEX008 | Bit.Blex.Generators | Error | Blex member must be a writable instance member
BLEX009 | Bit.Blex.Generators | Warning | Latest effect cannot cancel without a CancellationToken
BLEX010 | Bit.Blex.Generators | Error | Action must not be 'async void'
BLEX011 | Bit.Blex.Generators | Warning | Action return value is discarded
BLEX012 | Bit.Blex.Generators | Error | State field name equals generated property name
BLEX013 | Bit.Blex.Generators | Error | Unsupported parameter modifier
BLEX014 | Bit.Blex.Generators | Error | Generic action/effect methods are not supported
BLEX015 | Bit.Blex.Generators | Error | Store must be a plain class
BLEX016 | Bit.Blex.Generators | Error | Store cannot have another base class
BLEX017 | Bit.Blex.Generators | Error | Computed method must return a value
BLEX018 | Bit.Blex.Generators | Warning | Blex member attribute outside a store
BLEX019 | Bit.Blex.Generators | Error | Effect debounce cannot be negative
