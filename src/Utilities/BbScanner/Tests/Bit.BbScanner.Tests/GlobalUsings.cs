global using Microsoft.VisualStudio.TestTools.UnitTesting;
global using Bit.BbScanner.TestKit;

// The decoder is stateless across instances, so test classes run side by side. The classes that measure
// per-frame allocation opt out with [DoNotParallelize] and run on their own afterwards.
[assembly: Parallelize(Scope = ExecutionScope.ClassLevel)]
