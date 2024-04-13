using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NullableReferenceTypes.StrictMode;

internal class SyntaxRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitVariableDeclarator(VariableDeclaratorSyntax node) =>
        ReplaceAnnotatedNodes(node, base.VisitVariableDeclarator);

    public override SyntaxNode? VisitAssignmentExpression(AssignmentExpressionSyntax node) =>
        ReplaceAnnotatedNodes(node, base.VisitAssignmentExpression);

    public override SyntaxNode? VisitArgument(ArgumentSyntax node) =>
        ReplaceAnnotatedNodes(node, base.VisitArgument);

    public override SyntaxNode? VisitThrowExpression(ThrowExpressionSyntax node) =>
        ReplaceAnnotatedNodes(node, base.VisitThrowExpression);

    public override SyntaxNode? VisitThrowStatement(ThrowStatementSyntax node) =>
        ReplaceAnnotatedNodes(node, base.VisitThrowStatement);

    private static SyntaxNode? ReplaceAnnotatedNodes<TNode>(
        TNode node,
        Func<TNode, SyntaxNode?> defaultNodeVisitor
    )
        where TNode : SyntaxNode
    {
        SyntaxNode[] annotatedNodes = node.GetAnnotatedNodes(AnnotationKind.NullObliviousCode)
            .ToArray();

        if (annotatedNodes.Any() == false)
        {
            return defaultNodeVisitor(node);
        }

        return node.ReplaceNodes(
            annotatedNodes,
            (_, originalNode) =>
            {
                string? typeInfo = originalNode
                    .GetAnnotations(AnnotationKind.NullObliviousCode)
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
}
