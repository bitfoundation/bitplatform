; Unshipped analyzer releases; see https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
BLZ001 | Bit.BlazeDb.Generators | Error | Table type must be partial
BLZ002 | Bit.BlazeDb.Generators | Error | Table type must have exactly one [BlazeDbKey] property
BLZ003 | Bit.BlazeDb.Generators | Error | Unsupported property type
BLZ004 | Bit.BlazeDb.Generators | Error | Table type needs a parameterless constructor
BLZ005 | Bit.BlazeDb.Generators | Error | Duplicate field number
BLZ006 | Bit.BlazeDb.Generators | Error | Unsupported key type
BLZ007 | Bit.BlazeDb.Generators | Error | Unsupported table type shape
BLZ008 | Bit.BlazeDb.Generators | Error | Compound index references an unknown property
BLZ009 | Bit.BlazeDb.Generators | Error | Compound index needs at least two properties
BLZ010 | Bit.BlazeDb.Generators | Error | Duplicate index name
BLZ011 | Bit.BlazeDb.Generators | Error | Index name must be a valid identifier
BLZ012 | Bit.BlazeDb.Generators | Error | Table type inherits properties
BLZ013 | Bit.BlazeDb.Generators | Warning | Table type mixes explicit and automatic field numbers
BLZ014 | Bit.BlazeDb.Generators | Error | Field number out of range
BLZ015 | Bit.BlazeDb.Generators | Error | Property cannot be indexed
BLZ016 | Bit.BlazeDb.Generators | Error | Compound index lists a property twice
