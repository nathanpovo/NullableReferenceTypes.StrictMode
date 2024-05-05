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
open Microsoft.FSharp.Reflection

// https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/nullable-warnings
type DiagnosticId =
    /// Thrown value may be null.
    | CS8597
    /// Converting null literal or possible null value to non-nullable type.
    | CS8600
    /// Possible null reference assignment.
    | CS8601
    /// Dereference of a possibly null reference.
    | CS8602
    /// Possible null reference return.
    | CS8603
    /// Possible null reference argument for parameter.
    | CS8604
    /// Unboxing a possibly null value.
    | CS8605
    /// A possible null value may not be used for a type marked with [NotNull] or [DisallowNull]
    | CS8607
    /// Nullability of reference types in type doesn't match overridden member.
    | CS8608
    /// Nullability of reference types in return type doesn't match overridden member.
    | CS8609
    /// Nullability of reference types in type parameter doesn't match overridden member.
    | CS8610
    /// Nullability of reference types in type parameter doesn't match partial method declaration.
    | CS8611
    /// Nullability of reference types in type doesn't match implicitly implemented member.
    | CS8612
    /// Nullability of reference types in return type doesn't match implicitly implemented member.
    | CS8613
    /// Nullability of reference types in type of parameter doesn't match implicitly implemented member.
    | CS8614
    /// Nullability of reference types in type doesn't match implemented member.
    | CS8615
    /// Nullability of reference types in return type doesn't match implemented member.
    | CS8616
    /// Nullability of reference types in type of parameter doesn't match implemented member.
    | CS8617
    /// Non-nullable variable must contain a non-null value when exiting constructor. Consider declaring it as nullable.
    | CS8618
    /// Nullability of reference types in value doesn't match target type.
    | CS8619
    /// Argument cannot be used for parameter due to differences in the nullability of reference types.
    | CS8620
    /// Nullability of reference types in return type doesn't match the target delegate (possibly because of nullability attributes).
    | CS8621
    /// Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).
    | CS8622
    /// Argument cannot be used as an output due to differences in the nullability of reference types.
    | CS8624
    /// Cannot convert null literal to non-nullable reference type.
    | CS8625
    /// Nullable value type may be null.
    | CS8629
    /// The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match constraint type.
    | CS8631
    /// Nullability in constraints for type parameter of method doesn't match the constraints for type parameter of interface method. Consider using an explicit interface implementation instead.
    | CS8633
    /// The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'class' constraint.
    | CS8634
    /// Nullability of reference types in explicit interface specifier doesn't match interface implemented by the type.
    | CS8643
    /// Type does not implement interface member. Nullability of reference types in interface implemented by the base type doesn't match.
    | CS8644
    /// Member is already listed in the interface list on type with different nullability of reference types.
    | CS8645
    /// The switch expression does not handle some null inputs (it is not exhaustive).
    | CS8655
    /// Partial method declarations have inconsistent nullability in constraints for type parameter.
    | CS8667
    /// Object or collection initializer implicitly dereferences possibly null member.
    | CS8670
    /// The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
    | CS8714
    /// Parameter must have a non-null value when exiting.
    | CS8762
    /// A method marked [DoesNotReturn] should not return.
    | CS8763
    /// Nullability of return type doesn't match overridden member (possibly because of nullability attributes).
    | CS8764
    /// Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
    | CS8765
    /// Nullability of reference types in return type of doesn't match implicitly implemented member (possibly because of nullability attributes).
    | CS8766
    /// Nullability of reference types in type of parameter of doesn't match implicitly implemented member (possibly because of nullability attributes).
    | CS8767
    /// Nullability of reference types in return type doesn't match implemented member (possibly because of nullability attributes).
    | CS8768
    /// Nullability of reference types in type of parameter doesn't match implemented member (possibly because of nullability attributes).
    | CS8769
    /// Method lacks [DoesNotReturn] annotation to match implemented or overridden member.
    | CS8770
    /// Member must have a non-null value when exiting.
    | CS8774
    /// Member cannot be used in this attribute.
    | CS8776
    /// Member must have a non-null value when exiting.
    | CS8775
    /// Parameter must have a non-null value when exiting.
    | CS8777
    /// Nullability of reference types in return type doesn't match partial method declaration.
    | CS8819
    /// Parameter must have a non-null value when exiting because parameter is non-null.
    | CS8824
    /// Return value must be non-null because parameter is non-null.
    | CS8825
    /// The switch expression does not handle some null inputs (it is not exhaustive). However, a pattern with a 'when' clause might successfully match this value.
    | CS8847

let nullableDiagnostics =
    FSharpType.GetUnionCases(typeof<DiagnosticId>)
    |> Array.map (fun x -> FSharpValue.MakeUnion(x, [||]))
    |> Array.map (fun x -> x :?> DiagnosticId)

let nullableDiagnosticsAsString = nullableDiagnostics |> Array.map _.ToString()

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

    let markCodeWithDiagnostics (code: string) (diagnostics: DiagnosticResult seq) =
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

        let splitCode = code.Split Environment.NewLine

        let codeMarkedWithDiagnostics =
            diagnostics
            |> Seq.filter (_.HasLocation)
            |> Seq.map (_.Spans[0].Span)
            |> Seq.groupBy (_.StartLinePosition.Line)
            |> Seq.map (fun (key, values) -> createLineMarker values key)
            |> Seq.sortByDescending (_.line)
            |> Seq.fold (fun state y -> insertDiagnostic state y.line y.marker) splitCode
            |> (fun x -> String.Join(Environment.NewLine, x))

        codeMarkedWithDiagnostics

    let createAnalyserException innerException (codeUnderTest: string) (expectedDiagnostics: DiagnosticResult[]) =
        let codeMarkedWithDiagnostics =
            markCodeWithDiagnostics codeUnderTest expectedDiagnostics

        let message =
            "Expected diagnostics:"
            + Environment.NewLine
            + Environment.NewLine
            + String.Join(Environment.NewLine, expectedDiagnostics)
            + Environment.NewLine
            + Environment.NewLine
            + codeMarkedWithDiagnostics

        exn (message, innerException)

    let withId id (original: DiagnosticResult) =
        // This has to be done this way because DiagnosticResult does not provide any way of modifying the ID

        let withoutSpans =
            DiagnosticResult(id, original.Severity)
                .WithMessage(original.Message)
                .WithMessageFormat(original.MessageFormat)
                .WithArguments(original.MessageArguments)
                .WithOptions(original.Options)
                .WithIsSuppressed(original.IsSuppressed)

        original.Spans
        |> Seq.fold
            (fun (final: DiagnosticResult) location -> final.WithSpan(location.Span, location.Options))
            withoutSpans

    let toDiagnosticResult (diagnostic: Diagnostic) =
        DiagnosticResult(diagnostic.Id, diagnostic.Severity)
        |> _.WithMessage(diagnostic.GetMessage())
        |> _.WithIsSuppressed(diagnostic.IsSuppressed)
        |> _.WithSpan(diagnostic.Location.GetLineSpan())

    let mapDiagnosticId diagnosticId =
        if nullableDiagnosticsAsString |> Array.contains diagnosticId then
            "NRTSM_" + diagnosticId
        else
            diagnosticId

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

            let diagnosticResults = diagnostics |> Seq.map toDiagnosticResult |> Seq.toArray

            return (equivalentCode, diagnosticResults, -amountOfNewLines)
        }

    let ensureTestIsCorrect equivalentCode (diagnosticResults: DiagnosticResult array) =
        // Will only occur if the equivalent code was built incorrectly.
        // The equivalent code should always have diagnostics otherwise there would be no reason to test it.
        if diagnosticResults.Length = 0 then
            failwith (
                "No diagnostics found in:"
                + Environment.NewLine
                + Environment.NewLine
                + equivalentCode
            )

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
        let! equivalentCode, diagnosticResults, lineOffset = getExpectedDiagnostics source

        ensureTestIsCorrect equivalentCode diagnosticResults

        let mappedDiagnosticResults =
            diagnosticResults
            |> Array.map (fun diagnosticResult ->
                diagnosticResult
                |> _.WithLineOffset(lineOffset)
                |> withId (mapDiagnosticId diagnosticResult.Id))

        try
            return! VerifyDiagnosticAsync<'TAnalyzer, 'TVerifier> codeUnderTest mappedDiagnosticResults
        with e ->
            createAnalyserException e codeUnderTest mappedDiagnosticResults |> raise
    }
