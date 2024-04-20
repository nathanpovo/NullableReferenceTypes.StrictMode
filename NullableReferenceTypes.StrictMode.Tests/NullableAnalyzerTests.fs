namespace NullableReferenceTypes.StrictMode.Tests

open Microsoft.CodeAnalysis.Testing
open NullableReferenceTypes.StrictMode

// https://github.com/dotnet/roslyn-sdk/issues/1099#issuecomment-1723487931
type NullableAnalyzerTests =
    Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<NullableAnalyzer, DefaultVerifier>

module NullableAnalyzerTests =
    let VerifyNoDiagnosticAsync =
        AnalyzerTestFramework.VerifyNoDiagnosticAsync<NullableAnalyzer, DefaultVerifier>
