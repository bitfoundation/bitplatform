global using Microsoft.VisualStudio.TestTools.UnitTesting;

// The engine's tests are independent of each other - every one opens its own database - so they run in
// parallel, the way xUnit ran them class by class.
[assembly: Parallelize(Scope = ExecutionScope.ClassLevel)]
