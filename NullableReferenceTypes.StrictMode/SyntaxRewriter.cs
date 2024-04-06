using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NullableReferenceTypes.StrictMode;

internal class SyntaxRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitVariableDeclarator(VariableDeclaratorSyntax node)
    {
        SyntaxNode[] annotatedNodes = node.GetAnnotatedNodes(
                AnnotationKind.NullObliviousCodeAnnotationKind
            )
            .ToArray();

        if (annotatedNodes.Any() == false)
        {
            return base.VisitVariableDeclarator(node);
        }

        return ReplaceAnnotatedNodes(node, annotatedNodes);
    }

    public override SyntaxNode? VisitAssignmentExpression(AssignmentExpressionSyntax node)
    {
        SyntaxNode[] annotatedNodes = node.GetAnnotatedNodes(
                AnnotationKind.NullObliviousCodeAnnotationKind
            )
            .ToArray();

        if (annotatedNodes.Any() == false)
        {
            return base.VisitAssignmentExpression(node);
        }

        return ReplaceAnnotatedNodes(node, annotatedNodes);
    }

    public override SyntaxNode? VisitArgument(ArgumentSyntax node)
    {
        SyntaxNode[] annotatedNodes = node.GetAnnotatedNodes(
                AnnotationKind.NullObliviousCodeAnnotationKind
            )
            .ToArray();

        if (annotatedNodes.Any() == false)
        {
            return base.VisitArgument(node);
        }

        return ReplaceAnnotatedNodes(node, annotatedNodes);
    }

    private static SyntaxNode ReplaceAnnotatedNodes<TNode>(
        TNode node,
        IEnumerable<SyntaxNode> annotatedNodes
    )
        where TNode : SyntaxNode =>
        node.ReplaceNodes(
            annotatedNodes,
            (_, originalNode) =>
            {
                string? typeInfo = originalNode
                    .GetAnnotations(AnnotationKind.NullObliviousCodeAnnotationKind)
                    .Where(x => string.IsNullOrEmpty(x.Data) == false)
                    .Select(x => x.Data)
                    .SingleOrDefault();

                SyntaxNode newNode;
                if (typeInfo is not null)
                {
                    NullableTypeSyntax typeSyntax = SyntaxFactory.NullableType(
                        SyntaxFactory.ParseTypeName(typeInfo)
                    );

                    newNode = SyntaxFactory.CastExpression(
                        typeSyntax,
                        SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
                    );
                }
                else
                {
                    newNode = SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
                }

                return originalNode.CopyAnnotationsTo(newNode);
            }
        );
}
