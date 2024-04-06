using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NullableReferenceTypes.StrictMode;

internal static class ContextExtensions
{
    internal static void ReportDiagnostics(
        this SemanticModelAnalysisContext context,
        IEnumerable<Diagnostic> diagnostics
    )
    {
        foreach (Diagnostic diagnostic in diagnostics)
        {
            context.ReportDiagnostic(diagnostic);
        }
    }
}
