using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace NullableReferenceTypes.StrictMode;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NullableAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = DiagnosticConstants.NullableAnalyzer;

    // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
    // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
    private static readonly LocalizableString Title = new LocalizableResourceString(
        nameof(Resources.AnalyzerTitle),
        Resources.ResourceManager,
        typeof(Resources)
    );
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(
        nameof(Resources.AnalyzerMessageFormat),
        Resources.ResourceManager,
        typeof(Resources)
    );
    private static readonly LocalizableString Description = new LocalizableResourceString(
        nameof(Resources.AnalyzerDescription),
        Resources.ResourceManager,
        typeof(Resources)
    );
    private const string Category = "Usage";

    public static readonly DiagnosticDescriptor Descriptor =
        new(
            DiagnosticId,
            "Variable can be made constant",
            "Variable '{0}' can be made constant",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description
        );

    /// <summary>
    /// The diagnostic IDs to check for when checking the cloned compilation
    /// </summary>
    private static readonly string[] DiagnosticIds = { "CS8600", "CS8625" };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Descriptor);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSemanticModelAction(Action);
    }

    private static void Action(SemanticModelAnalysisContext context)
    {
        CancellationToken cancellationToken = context.CancellationToken;

        SemanticModel semanticModel = context.SemanticModel;
        SyntaxTree syntaxTree = semanticModel.SyntaxTree;
        SyntaxNode syntaxNode = syntaxTree.GetRoot(cancellationToken);

        SyntaxNode annotatedSyntaxNode = new NullObliviousCodeAnnotator(semanticModel).Visit(
            syntaxNode
        );

        SyntaxNode nullifiedSyntaxNode = new SyntaxRewriter().Visit(annotatedSyntaxNode);

        ImmutableArray<Diagnostic> compilationCloneDiagnostics = semanticModel
            .Compilation.Clone()
            .ReplaceSyntaxTree(syntaxTree, nullifiedSyntaxNode.SyntaxTree)
            .GetDiagnostics();

        IEnumerable<Diagnostic> diagnostics = compilationCloneDiagnostics
            .Where(x => DiagnosticIds.Contains(x.Id))
            .Select(x => GetOriginalNodeSpan(annotatedSyntaxNode, nullifiedSyntaxNode, x))
            .Where(x => x is not null)
            .Select(x => x!.Value)
            .Select(x => Diagnostic.Create(Descriptor, Location.Create(syntaxTree, x)));

        context.ReportDiagnostics(diagnostics);
    }

    /// <summary>
    /// Tries to get the original text span of node that is covered by the diagnostic in the modified compilation
    /// </summary>
    private static TextSpan? GetOriginalNodeSpan(
        SyntaxNode annotatedSyntaxNode,
        SyntaxNode modifiedSyntaxNode,
        Diagnostic diagnostic
    )
    {
        SyntaxNode modifiedNode = modifiedSyntaxNode.FindNode(
            diagnostic.Location.SourceSpan,
            getInnermostNodeForTie: true
        );

        SyntaxAnnotation? annotation = modifiedNode
            .GetAnnotations(AnnotationKind.NullObliviousCodeAnnotationKind)
            .SingleOrDefault();

        if (annotation is null)
        {
            return null;
        }

        return annotatedSyntaxNode.GetAnnotatedNodes(annotation).SingleOrDefault()?.Span;
    }
}
