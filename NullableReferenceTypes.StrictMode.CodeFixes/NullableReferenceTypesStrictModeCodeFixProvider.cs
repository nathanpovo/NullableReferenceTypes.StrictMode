using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace NullableReferenceTypes.StrictMode;

[
    ExportCodeFixProvider(
        LanguageNames.CSharp,
        Name = nameof(NullableReferenceTypesStrictModeCodeFixProvider)
    ),
    Shared
]
public class NullableReferenceTypesStrictModeCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(NullableAnalyzer.DiagnosticId);

    public sealed override FixAllProvider GetFixAllProvider() =>
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
        WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode root = await context
            .Document.GetSyntaxRootAsync(context.CancellationToken)
            .ConfigureAwait(false);

        // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
        Diagnostic diagnostic = context.Diagnostics.First();
        TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the type declaration identified by the diagnostic.
        TypeDeclarationSyntax declaration = root.FindToken(diagnosticSpan.Start)
            .Parent.AncestorsAndSelf()
            .OfType<TypeDeclarationSyntax>()
            .First();

        // Register a code action that will invoke the fix.
        context.RegisterCodeFix(
            CodeAction.Create(
                title: CodeFixResources.CodeFixTitle,
                createChangedSolution: c => MakeUppercaseAsync(context.Document, declaration, c),
                equivalenceKey: nameof(CodeFixResources.CodeFixTitle)
            ),
            diagnostic
        );
    }

    private static async Task<Solution> MakeUppercaseAsync(
        Document document,
        TypeDeclarationSyntax typeDecl,
        CancellationToken cancellationToken
    )
    {
        // Compute new uppercase name.
        SyntaxToken identifierToken = typeDecl.Identifier;
        string newName = identifierToken.Text.ToUpperInvariant();

        // Get the symbol representing the type to be renamed.
        SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken);
        INamedTypeSymbol typeSymbol = semanticModel.GetDeclaredSymbol(typeDecl, cancellationToken);

        // Produce a new solution that has all references to that type renamed, including the declaration.
        Solution originalSolution = document.Project.Solution;
        Microsoft.CodeAnalysis.Options.OptionSet optionSet = originalSolution.Workspace.Options;
        Solution newSolution = await Renamer
            .RenameSymbolAsync(
                document.Project.Solution,
                typeSymbol,
                newName,
                optionSet,
                cancellationToken
            )
            .ConfigureAwait(false);

        // Return the new solution with the now-uppercase type name.
        return newSolution;
    }
}
