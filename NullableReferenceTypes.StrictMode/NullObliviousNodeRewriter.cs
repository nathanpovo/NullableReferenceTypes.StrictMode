using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NullableReferenceTypes.StrictMode;

internal static class NullObliviousNodeRewriter
{
    public static SyntaxNode ReplaceAnnotatedNodes<TNode>(this TNode node)
        where TNode : SyntaxNode
    {
        SyntaxNode[] annotatedNodes = node.GetAnnotatedNodes(AnnotationKind.NullObliviousCode)
            .ToArray();

        if (annotatedNodes.Any() == false)
        {
            return node;
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
