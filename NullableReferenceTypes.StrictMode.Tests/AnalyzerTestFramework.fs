module AnalyzerTestFramework

open System
open System.Collections.Immutable
open System.Text
open System.Threading
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp.Testing
open Microsoft.CodeAnalysis.Diagnostics
open Microsoft.CodeAnalysis.Testing
open Microsoft.CodeAnalysis.Testing.Model
open Microsoft.CodeAnalysis.Text

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

    type DiagnosticGetter() as this =
        inherit CSharpAnalyzerTest<EmptyDiagnosticAnalyzer, DefaultVerifier>()

        do
            // Specify that call compiler diagnostics should be reported
            this.CompilerDiagnostics <- CompilerDiagnostics.All

            // Disables the CS1591 diagnostics (Missing XML comment for publicly visible type or member 'XYZ') because it is
            // not needed.
            this.DisabledDiagnostics.Add "CS1591"

        static let additionalProjects = ImmutableArray<EvaluatedProjectState>.Empty
        static let analyzers = ImmutableArray<DiagnosticAnalyzer>.Empty
        static let additionalDiagnostics = ImmutableArray<struct (Project * Diagnostic)>.Empty

        member _.verifier = DiagnosticGetter.Verify

        // :(
        // https://github.com/dotnet/fsharp/issues/12448
        member private _.GetSortedDiagnostics
            (solution, analyzers, additionalDiagnostics, compilerDiagnostics, verifier, cancellationToken)
            =
            base.GetSortedDiagnosticsAsync(
                solution,
                analyzers,
                additionalDiagnostics,
                compilerDiagnostics,
                verifier,
                cancellationToken
            )

        member this.GetDiagnostics(source, cancellationToken: CancellationToken) =
            this.TestCode <- source

            let primaryProject = EvaluatedProjectState(this.TestState, this.ReferenceAssemblies)

            let createProjectTask =
                base.CreateProjectAsync(primaryProject, additionalProjects, cancellationToken)

            task {
                let! project = createProjectTask

                let! sortedDiagnostics =
                    this.GetSortedDiagnostics(
                        project.Solution,
                        analyzers,
                        additionalDiagnostics,
                        this.CompilerDiagnostics,
                        this.verifier,
                        cancellationToken
                    )

                return
                    sortedDiagnostics
                    |> Seq.map (fun struct (_, diagnostic) -> diagnostic)
                    |> Seq.toArray
            }

    let createAnalyserException innerException (codeUnderTest: string) (expectedDiagnostics: DiagnosticResult[]) =
        let applyMarkersForDiagnostic (state: char array) startCharacter endCharacter =
            for i = startCharacter to (endCharacter - 1) do
                state[i] <- '^'

            state

        // Note that there could be multiple diagnostics on the same line
        // This function handles that scenario
        let createLineMarker (locations: FileLinePositionSpan seq) line =
            let characterPositions =
                locations
                |> Seq.map (fun x ->
                    {| Start = x.StartLinePosition.Character
                       End = x.EndLinePosition.Character |})
                |> Seq.cache

            let maxEndCharacter = characterPositions |> Seq.map (_.End) |> Seq.max

            let initialState = ' ' |> Array.create maxEndCharacter

            let marker =
                characterPositions
                |> Seq.fold
                    (fun state location -> applyMarkersForDiagnostic state location.Start location.End)
                    initialState
                |> String

            {| line = line; marker = marker |}

        // The diagnostic marker has to be inserted in the line after (meaning below) the actual diagnostic
        let insertDiagnostic (code: string array) line marker =
            code |> Array.insertAt (line + 1) marker

        let splitCode = codeUnderTest.Split Environment.NewLine

        let codeMarkedWithDiagnostics =
            expectedDiagnostics
            |> Seq.filter (_.HasLocation)
            |> Seq.map (_.Spans[0].Span)
            |> Seq.groupBy (_.StartLinePosition.Line)
            |> Seq.map (fun (key, values) -> createLineMarker values key)
            |> Seq.sortByDescending (_.line)
            |> Seq.fold (fun state y -> insertDiagnostic state y.line y.marker) splitCode
            |> (fun x -> String.Join(Environment.NewLine, x))

        let message =
            "Expected diagnostics:"
            + Environment.NewLine
            + Environment.NewLine
            + String.Join(Environment.NewLine, expectedDiagnostics)
            + Environment.NewLine
            + Environment.NewLine
            + codeMarkedWithDiagnostics

        exn (message, innerException)

    let mapDiagnostic lineDifference (diagnostic: Diagnostic) =
        let lineSpan = diagnostic.Location.GetLineSpan()

        let newLinePosition (linePosition: LinePosition, lineDifference) =
            LinePosition(linePosition.Line + lineDifference, linePosition.Character)

        let newLineSpan =
            FileLinePositionSpan(
                lineSpan.Path,
                newLinePosition (lineSpan.StartLinePosition, lineDifference),
                newLinePosition (lineSpan.EndLinePosition, lineDifference)
            )

        DiagnosticResult("NRTSM_" + diagnostic.Id, diagnostic.Severity)
        |> _.WithMessage(diagnostic.GetMessage())
        |> _.WithIsSuppressed(diagnostic.IsSuppressed)
        |> _.WithSpan(newLineSpan)

    let getDiagnostics source =
        DiagnosticGetter().GetDiagnostics(source, CancellationToken.None)

    let getExpectedDiagnostics (source: string) =
        let amountOfNewLines = 2

        // Create code equivalent to the code under test that would produce the expected diagnostics
        let equivalentCode =
            let builder = StringBuilder()

            builder.Append "#define EQUIVALENT_CODE" |> ignore

            Environment.NewLine
            |> Seq.replicate amountOfNewLines
            |> Seq.iter (fun x -> builder.Append x |> ignore)

            builder.Append source |> ignore

            builder.ToString()

        task {
            let! diagnostics = getDiagnostics equivalentCode

            let diagnosticResults =
                diagnostics |> Seq.map (mapDiagnostic -amountOfNewLines) |> Seq.toArray

            // Will only occur if the equivalent code was built incorrectly.
            // The equivalent code should always have diagnostics otherwise there would be no reason to test it.
            if diagnosticResults.Length = 0 then
                failwith (
                    "No diagnostics found in:"
                    + Environment.NewLine
                    + Environment.NewLine
                    + equivalentCode
                )

            return diagnosticResults
        }

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

/// <summary>
/// An analyzer tester that verifies that all diagnostics related to nullable reference type are reported at the
/// interaction points between nullable-enabled and nullable-disabled code sections.
/// </summary>
/// <remarks>
/// <para>
/// The function operates by dividing the source code into two separate compilations:
/// </para>
/// <list type="table">
/// <item>
/// <term>Code under test:</term>
/// <description>
/// This is the actual source code being written and tested.
/// </description>
/// </item>
/// <item>
/// <term>Equivalent code:</term>
/// <description>
/// A simulated version of the code that triggers all expected diagnostics for nullable reference types.
/// </description>
/// </item>
/// </list>
/// <para>
/// The analyzer tester uses the <c>EQUIVALENT_CODE</c> preprocessor directive to conditionally compile the source code,
/// distinguishing between the code under test and the equivalent code.
/// </para>
/// <para>
/// The tester uses the diagnostics reported on the equivalent code to know what diagnostics should be reported on the
/// actual code under test. This makes the analyzer tester more accurate as-opposed to manually defining the expected
/// diagnostics in each test.
/// </para>
/// </remarks>
let VerifyStrictFlowAnalysisDiagnosticsAsync<'TAnalyzer, 'TVerifier
    when 'TAnalyzer: (new: unit -> 'TAnalyzer)
    and 'TAnalyzer :> DiagnosticAnalyzer
    and 'TVerifier: (new: unit -> 'TVerifier)
    and 'TVerifier :> IVerifier>
    (source: string)
    =
    let codeUnderTest = source.ReplaceLineEndings()

    task {
        let! diagnosticResults = getExpectedDiagnostics source

        try
            return! VerifyDiagnosticAsync<'TAnalyzer, 'TVerifier> codeUnderTest diagnosticResults
        with e ->
            createAnalyserException e codeUnderTest diagnosticResults |> raise
    }
