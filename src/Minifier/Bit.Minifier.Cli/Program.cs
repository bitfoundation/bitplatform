using Bit.Minifier;

// usage: bit-minifier --decode <map file> [<stack trace file>]
// Reads a stack trace (from the file, or from standard input) and writes it back with the names the source has.
// Minifying is the other program's, the one the Bit.Minifier package runs on publish: this one is for whoever
// holds a map, and needs neither the project nor the assemblies.
const string Usage = "usage: bit-minifier --decode <map file> [<stack trace file>]";

if (args is ["--decode", ..]) return Decode.Run(Usage, args);

Console.Error.WriteLine(Usage);
return 2;
