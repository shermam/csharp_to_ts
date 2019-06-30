using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace csharp_to_ts
{
  class PropertyCollector : CSharpSyntaxWalker
  {
    public readonly List<PropertyDeclarationSyntax> Properties = new List<PropertyDeclarationSyntax>();
    public readonly List<ClassDeclarationSyntax> Classes = new List<ClassDeclarationSyntax>();

    public override void Visit(SyntaxNode node)
    {
      if (node is PropertyDeclarationSyntax)
      {
        this.Properties.Add(node as PropertyDeclarationSyntax);
      }
      else if (node is ClassDeclarationSyntax)
      {
        this.Classes.Add(node as ClassDeclarationSyntax);
      }
      base.Visit(node);
    }
  }
}
