module AnalyzerTestFramework

open System
open System.Collections.Immutable
open System.Threading
open Microsoft.CodeAnalysis.CSharp.Testing
open Microsoft.CodeAnalysis.Diagnostics
open Microsoft.CodeAnalysis.Testing
open Microsoft.CodeAnalysis.Testing.Model

[<AutoOpen>]
module private PrivateHelpers =
    type NoDiagnosticsAnalyzerTest<'TAnalyzer, 'TVerifier
        when 'TAnalyzer: (new: unit -> 'TAnalyzer)
        and 'TAnalyzer :> DiagnosticAnalyzer
        and 'TVerifier: (new: unit -> 'TVerifier)
        and 'TVerifier :> IVerifier>() =
        inherit CSharpAnalyzerTest<'TAnalyzer, 'TVerifier>()

        member _.verifier = NoDiagnosticsAnalyzerTest<'TAnalyzer, 'TVerifier>.Verify

        override this.RunImplAsync(cancellationToken: CancellationToken) =
            // The following is based on the original implementation of the analyzer tester.
            //
            // The main (and only) difference is that this tester ensures that absolutely no diagnostics are reported on
            // the code under test.
            //
            // The reason why this is required is that the original tester has no way of explicitly being told that no
            // diagnostics should be reported on the code under test.
            //
            // The original tester's success criteria relied solely on diagnostic matching (matching the reported
            // diagnostics to the diagnostics passed to the tester or the diagnostics marked in the code under test),
            // potentially overlooking instances where the code under test should yield no diagnostics.

            let analyzers = this.GetDiagnosticAnalyzers() |> Seq.toArray
            let defaultDiagnostic = this.GetDefaultDiagnostic analyzers

            let supportedDiagnostics =
                analyzers
                |> Seq.collect (_.SupportedDiagnostics)
                |> Seq.toArray
                |> (_.ToImmutableArray())

            let fixableDiagnostics = ImmutableArray<string>.Empty

            let testState =
                this.TestState
                    .WithInheritedValuesApplied(null, fixableDiagnostics)
                    .WithProcessedMarkup(
                        this.MarkupOptions,
                        defaultDiagnostic,
                        supportedDiagnostics,
                        fixableDiagnostics,
                        this.DefaultFilePath
                    )

            this.verifier.NotEmpty($"{nameof this.TestState}.{nameof testState.Sources}", this.TestState.Sources)

            // Clear out all the expected diagnostics to ensure that no diagnostics are reported on the code under test
            testState.ExpectedDiagnostics.Clear()

            let primaryProject = EvaluatedProjectState(testState, this.ReferenceAssemblies)

            let additionalProjects =
                testState.AdditionalProjects.Values
                |> Seq.map (fun x -> EvaluatedProjectState(x, this.ReferenceAssemblies))
                |> _.ToImmutableArray()

            let verifyTask =
                base.VerifyDiagnosticsAsync(
                    primaryProject,
                    additionalProjects,
                    testState.ExpectedDiagnostics.ToArray(),
                    this.verifier,
                    cancellationToken
                )

            task { return! verifyTask }

let VerifyNoDiagnosticAsync<'TAnalyzer, 'TVerifier
    when 'TAnalyzer: (new: unit -> 'TAnalyzer)
    and 'TAnalyzer :> DiagnosticAnalyzer
    and 'TVerifier: (new: unit -> 'TVerifier)
    and 'TVerifier :> IVerifier>
    (source: string)
    =
    AnalyzerVerifier<'TAnalyzer, NoDiagnosticsAnalyzerTest<'TAnalyzer, 'TVerifier>, 'TVerifier>
        .VerifyAnalyzerAsync(source)

let VerifyDiagnosticAsync<'TAnalyzer, 'TVerifier
    when 'TAnalyzer: (new: unit -> 'TAnalyzer)
    and 'TAnalyzer :> DiagnosticAnalyzer
    and 'TVerifier: (new: unit -> 'TVerifier)
    and 'TVerifier :> IVerifier>
    (source: string)
    (expected: DiagnosticResult[])
    =
    CSharpAnalyzerVerifier<'TAnalyzer, 'TVerifier>
        .VerifyAnalyzerAsync(source, expected)
