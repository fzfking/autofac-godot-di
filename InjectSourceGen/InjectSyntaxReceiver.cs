using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutofacGodotDi
{
    public class InjectSyntaxReceiver : ISyntaxReceiver
    {
        public List<MethodDeclarationSyntax> CandidateMethods { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is MethodDeclarationSyntax { AttributeLists: { Count: > 0 } } methodDeclaration)
            {
                CandidateMethods.Add(methodDeclaration);
            }
        }
    }
}