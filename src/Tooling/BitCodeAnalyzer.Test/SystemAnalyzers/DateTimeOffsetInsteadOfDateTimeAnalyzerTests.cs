using Bit.Tooling.CodeAnalyzer.SystemAnalyzers;
using Bit.Tooling.CodeAnalyzer.SystemCodeFixes;
using Bit.Tooling.CodeAnalyzer.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Bit.Tooling.CodeAnalyzer.Test.SystemAnalyzers
{
    [TestClass]
    public class DateTimeOffsetInsteadOfDateTimeAnalyzerTests : CodeFixVerifier
    {
        [TestMethod]
        [TestCategory("Analyzer")]
        public async Task FindDateTimeUsageTest()
        {
            const string sourceCodeWithDateTimeUsage = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace MyNamespace
    {
        class MyClass
        {   
            public MyClass()
            {
                DateTime a = DateTime.Now;
            }
        }
    }";
            DiagnosticResult firstDateTimeUsage = new DiagnosticResult
            {
                Id = nameof(DateTimeOffsetInsteadOfDateTimeAnalyzer),
                Message = DateTimeOffsetInsteadOfDateTimeAnalyzer.Message,
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation(15, 17) }
            };

            DiagnosticResult secondDateTimeUsage = new DiagnosticResult
            {
                Id = nameof(DateTimeOffsetInsteadOfDateTimeAnalyzer),
                Message = DateTimeOffsetInsteadOfDateTimeAnalyzer.Message,
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation(15, 30) }
            };

            await VerifyCSharpDiagnostic(sourceCodeWithDateTimeUsage, firstDateTimeUsage, secondDateTimeUsage);
        }

        [TestMethod]
        [TestCategory("CodeFixProvider")]
        public async Task FixDateTimeUsageTest()
        {
            const string sourceCodeWithDateTimeUsage = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace MyNamespace
    {
        class MyClass
        {   
            public MyClass()
            {
                DateTime a = DateTime.Now;
            }
        }
    }";

            const string fixedSourceCode = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace MyNamespace
    {
        class MyClass
        {   
            public MyClass()
            {
                DateTimeOffset a = DateTimeOffset.Now;
            }
        }
    }";

            await VerifyCSharpFix(sourceCodeWithDateTimeUsage, fixedSourceCode);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new DateTimeOffsetInsteadOfDateTimeCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new DateTimeOffsetInsteadOfDateTimeAnalyzer();
        }
    }
}
