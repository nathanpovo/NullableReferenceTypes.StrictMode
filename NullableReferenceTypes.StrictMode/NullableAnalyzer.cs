using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

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

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Descriptor);

    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
        {
            return;
        }

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // analyzer that checks if a value from a null-oblivious context is assigned to a non-nullable variable
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.LocalDeclarationStatement);

        // Handling expression statements (e.g. x = GetNullableValue())
        context.RegisterSyntaxNodeAction(AnalyzeExpressionNode, SyntaxKind.ExpressionStatement);
    }

    private void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        // Guard clause to ensure we're only analyzing local declarations
        if (context.Node is not LocalDeclarationStatementSyntax localDeclaration)
        {
            return;
        }

        SemanticModel model = context.SemanticModel;

        VariableDeclarationSyntax variableDeclarationSyntax = localDeclaration.Declaration;

        // Ensure the variable declaration is not null otherwise we can't analyze it
        if (variableDeclarationSyntax?.Type is not { } variableType)
        {
            return;
        }

        // There is no need to analyze var types since they are implicitly typed (and therefore nullable)
        if (variableType.IsVar)
        {
            return;
        }

        NullableTypeSyntax nullableType = variableType as NullableTypeSyntax;
        foreach (VariableDeclaratorSyntax variable in variableDeclarationSyntax.Variables)
        {
            // This is what is on the right side of the equals sign
            ExpressionSyntax initializerValue = variable?.Initializer?.Value;

            if (initializerValue is null)
            {
                continue;
            }

            // The initializer symbol can be different depending on the type of the initializer
            ISymbol initializerValueSymbol = model.GetSymbolInfo(initializerValue).Symbol;

            if (
                initializerValueSymbol is IPropertySymbol
                {
                    NullableAnnotation: NullableAnnotation.None
                }
            )
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Descriptor, initializerValue.GetLocation(), initializerValue)
                );
            }

            if (
                initializerValueSymbol is IFieldSymbol
                {
                    NullableAnnotation: NullableAnnotation.None
                }
            )
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Descriptor, initializerValue.GetLocation(), initializerValue)
                );
            }

            if (
                initializerValueSymbol
                    is IMethodSymbol
                    {
                        ReturnNullableAnnotation: NullableAnnotation.None,
                        IsGenericMethod: false
                    }
                && nullableType is null
            )
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Descriptor, initializerValue.GetLocation(), initializerValue)
                );
            }

            // Generic methods are a special case since they can be declared as "public T GetValue<T>()"
            // This confuses the analyzer check above since it thinks that T is a properly annotated type
            // This is a special check that checks if the definition of the method itself was annotated rather than checking the return type
            if (
                initializerValueSymbol
                    is IMethodSymbol
                    {
                        OriginalDefinition.ReturnNullableAnnotation: NullableAnnotation.None,
                        IsGenericMethod: true
                    }
                && nullableType is null
            )
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Descriptor, initializerValue.GetLocation(), initializerValue)
                );
            }
        }
    }

    private void AnalyzeExpressionNode(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not ExpressionStatementSyntax localDeclaration)
        {
            return;
        }

        if (
            localDeclaration.Expression is not AssignmentExpressionSyntax assignmentExpressionSyntax
        )
        {
            return;
        }

        SemanticModel model = context.SemanticModel;

        // The type being assigned to
        // Can be a field or a property
        _ = model.GetSymbolInfo(assignmentExpressionSyntax.Left).Symbol;

        // This is what is on the right side of the equals sign
        ExpressionSyntax initializerValue = assignmentExpressionSyntax.Right;

        if (initializerValue is null)
        {
            return;
        }

        // The initializer symbol can be different depending on the type of the initializer
        ISymbol initializerValueSymbol = model.GetSymbolInfo(initializerValue).Symbol;

        if (
            initializerValueSymbol is IPropertySymbol
            {
                NullableAnnotation: NullableAnnotation.None
            }
        )
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Descriptor, initializerValue.GetLocation(), initializerValue)
            );
        }

        if (initializerValueSymbol is IFieldSymbol { NullableAnnotation: NullableAnnotation.None })
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Descriptor, initializerValue.GetLocation(), initializerValue)
            );
        }

        if (
            initializerValueSymbol is IMethodSymbol
            {
                ReturnNullableAnnotation: NullableAnnotation.None,
                IsGenericMethod: false
            }
        )
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Descriptor, initializerValue.GetLocation(), initializerValue)
            );
        }

        // Generic methods are a special case since they can be declared as "public T GetValue<T>()"
        // This confuses the analyzer check above since it thinks that T is a properly annotated type
        // This is a special check that checks if the definition of the method itself was annotated rather than checking the return type
        if (
            initializerValueSymbol is IMethodSymbol
            {
                OriginalDefinition.ReturnNullableAnnotation: NullableAnnotation.None,
                IsGenericMethod: true
            }
        )
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Descriptor, initializerValue.GetLocation(), initializerValue)
            );
        }
    }
}
