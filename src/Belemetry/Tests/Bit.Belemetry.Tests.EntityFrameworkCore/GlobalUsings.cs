global using System.Diagnostics;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.VisualStudio.TestTools.UnitTesting;

// ActivityListener is process-wide, so the tests run one at a time.
[assembly: DoNotParallelize]
