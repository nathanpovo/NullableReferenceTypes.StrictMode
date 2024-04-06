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
                originalNode.CopyAnnotationsTo(
                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
                )
        );
}
