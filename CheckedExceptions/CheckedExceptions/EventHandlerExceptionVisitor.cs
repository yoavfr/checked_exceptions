using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace CheckedExceptions
{
    class EventHandlerExceptionVisitor : CSharpSyntaxWalker
    {
        public HashSet<EventHandlerExceptionViolation> Exceptions { get; }
        private Compilation Compilation { get; }
        private SemanticModel SemanticModel { get; }
        private SyntaxNode BodySyntax { get; }

        public EventHandlerExceptionVisitor(Compilation compilation, SemanticModel semanticModel, SyntaxNode bodySyntax)
        {
            Exceptions = new HashSet<EventHandlerExceptionViolation>();
            Compilation = compilation;
            SemanticModel = semanticModel;
            BodySyntax = bodySyntax;
        }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            var assignedIdentifier = node.Left as IdentifierNameSyntax;
            if (assignedIdentifier == null)
            {
                assignedIdentifier = node.Left.DescendantNodes().OfType<IdentifierNameSyntax>().LastOrDefault();
            }
            if (assignedIdentifier != null)
            {
                if (node.Kind() == SyntaxKind.AddAssignmentExpression || node.Kind() == SyntaxKind.SubtractAssignmentExpression)
                {
                    var assignedSymbol = SemanticModel.GetSymbolInfo(assignedIdentifier);
                    if (assignedSymbol.Symbol.Kind == SymbolKind.Event)
                    {
                        var eventHandler = node.Right as IdentifierNameSyntax;
                        if (eventHandler == null)
                        {
                            eventHandler = node.Right.DescendantNodes().OfType<IdentifierNameSyntax>().LastOrDefault();
                        }
                        if (eventHandler != null)
                        {
                            var eventHandlerSymbol = SemanticModel.GetSymbolInfo(eventHandler);
                            var declaredExceptions = new DocumentationComment(Compilation).GetDeclaredExceptions(eventHandlerSymbol.Symbol, true);
                            foreach (var exception in declaredExceptions)
                            {
                                Exceptions.Add(new EventHandlerExceptionViolation(exception, node.Right, eventHandlerSymbol.Symbol.Name));
                            }
                        }
                    }
                }
            }
            base.VisitAssignmentExpression(node);
        }
    }
}
