global using Microsoft.VisualStudio.TestTools.UnitTesting;

// Every test opens databases of its own, so classes run in parallel.
[assembly: Parallelize(Scope = ExecutionScope.ClassLevel)]
