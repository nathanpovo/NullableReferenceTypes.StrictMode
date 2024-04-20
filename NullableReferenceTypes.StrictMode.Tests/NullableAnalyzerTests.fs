namespace NullableReferenceTypes.StrictMode.Tests

open System
open Microsoft.CodeAnalysis.Testing
open NullableReferenceTypes.StrictMode

// https://github.com/dotnet/roslyn-sdk/issues/1099#issuecomment-1723487931
module NullableAnalyzerTests =
    let VerifyNoDiagnosticAsync =
        AnalyzerTestFramework.VerifyNoDiagnosticAsync<NullableAnalyzer, DefaultVerifier>

    let VerifyDiagnosticAsync source ([<ParamArray>] diagnostics) =
        AnalyzerTestFramework.VerifyDiagnosticAsync<NullableAnalyzer, DefaultVerifier> source diagnostics
