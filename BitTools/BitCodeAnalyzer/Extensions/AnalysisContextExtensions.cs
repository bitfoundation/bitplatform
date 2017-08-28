using System.Reflection;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    public static class AnalysisContextExtensions
    {
        public static void BitEnableConcurrentExecution(this AnalysisContext context)
        {
            context.GetType().GetTypeInfo().GetMethod("EnableConcurrentExecution")?.Invoke(context, new object[] { });
        }
    }
}
