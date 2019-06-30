using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace csharp_to_ts
{
  class UsingCollector : CSharpSyntaxWalker
  {
    public readonly List<UsingDirectiveSyntax> Usings = new List<UsingDirectiveSyntax>();

    public override void VisitUsingDirective(UsingDirectiveSyntax node)
    {
      if (node.Name.ToString() != "System" &&
                      !node.Name.ToString().StartsWith("System."))
      {
        this.Usings.Add(node);
      }
    }
  }
}
