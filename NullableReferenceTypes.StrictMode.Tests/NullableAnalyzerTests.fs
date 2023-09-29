namespace NullableReferenceTypes.StrictMode.Tests

open NullableReferenceTypes.StrictMode
open Roslynator.Testing
open Roslynator.Testing.CSharp.Xunit

type NullableAnalyzerTests() =
    inherit XunitDiagnosticVerifier<NullableAnalyzer, EmptyCodeFixProvider>()

    override this.get_Descriptor() = NullableAnalyzer.Descriptor
