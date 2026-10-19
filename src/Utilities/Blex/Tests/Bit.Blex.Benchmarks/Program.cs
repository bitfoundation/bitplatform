using System.Reflection;
using BenchmarkDotNet.Running;

// Runs every *Benchmarks class in this assembly. Pass BenchmarkDotNet CLI args to filter, e.g.
//   dotnet run -c Release -- --filter *Dispatch*
//   dotnet run -c Release -- --list flat
// With no args, an interactive menu lets you pick which suite(s) to run.
BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly()).Run(args);
