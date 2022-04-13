using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace CheckedExceptions
{
    class UnhandledExceptionVisitor : CSharpSyntaxWalker
    {
        public HashSet<UnhandledExceptionViolation> Exceptions { get; }
        private Compilation Compilation { get; }
        private SemanticModel SemanticModel { get; }
        private SyntaxNode BodySyntax { get; }

        public UnhandledExceptionVisitor(Compilation compilation, SemanticModel semanticModel, SyntaxNode bodySyntax)
        {
            Exceptions = new HashSet<UnhandledExceptionViolation>();
            Compilation = compilation;
            SemanticModel = semanticModel;
            BodySyntax = bodySyntax;
        }

        /// <summary>
        /// Declared exceptions for method invocations. E.g. Foo()
        /// </summary>
        /// <param name="node"></param>
        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            AnalyzeInvocationTargetComment(node);
            base.VisitInvocationExpression(node);
        }

        /// <summary>
        /// Declared exceptions for Getters and Setters. E.g. Program.Foo
        /// </summary>
        /// <param name="node"></param>
        public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            AnalyzeInvocationTargetComment(node);
            base.VisitMemberAccessExpression(node);
        }

        /// <summary>
        /// Declared exceptions on constructors
        /// </summary>
        /// <param name="node"></param>
        public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            AnalyzeInvocationTargetComment(node);
            base.VisitObjectCreationExpression(node);
        }

        /// <summary>
        /// Look for exceptions thrown in the body of the method or in a lambda
        /// </summary>
        /// <param name="node"></param>
        public override void VisitThrowStatement(ThrowStatementSyntax node)
        {
            var controlFlowAnalysis = SemanticModel.AnalyzeControlFlow(node);
            if (controlFlowAnalysis.StartPointIsReachable)
            {
                AddThrown(node, node.Expression);
            }
            base.VisitThrowStatement(node);
        }

        /// <summary>
        /// Look for exceptions thrown in accessor expressions e.g. private bool Foo => thrown new FieldAccessException()
        /// </summary>
        /// <param name="node"></param>
        public override void VisitThrowExpression(ThrowExpressionSyntax node)
        {
            AddThrown(node, node.Expression);
            base.VisitThrowExpression(node);
        }

        private void AnalyzeInvocationTargetComment(ExpressionSyntax node)
        {
            var identifiers = (IEnumerable<ExpressionSyntax>)node.DescendantNodes().OfType<IdentifierNameSyntax>();
            foreach (var expression in identifiers)
            {
                var expressionSymbol = SemanticModel.GetSymbolInfo(expression).Symbol;
                if (expressionSymbol == null)
                {
                    expressionSymbol = SemanticModel.GetSymbolInfo(expression).CandidateSymbols.FirstOrDefault();
                }
                if (expressionSymbol != null)
                {
                    if (expressionSymbol.Kind != SymbolKind.Method && expressionSymbol.Kind != SymbolKind.Property)
                    {
                        continue;
                    }
                    var accessorKind = expressionSymbol.Kind == SymbolKind.Property ? GetAccessorKind(expression) : AccessorKind.None;
                    var declaredExceptions = new DocumentationComment(Compilation).GetDeclaredExceptions(expressionSymbol, true, accessorKind);

                    SyntaxNode current = expression;
                    while (current != null && !(current is StatementSyntax || current is PropertyDeclarationSyntax))
                    {
                        current = current.Parent;
                    }
                    if (current != null)
                    {
                        foreach (var declaredException in declaredExceptions)
                        {
                            if (!IsExceptionCaught(node, declaredException))
                            {
                                {
                                    Exceptions.Add(new UnhandledExceptionViolation(declaredException, current));
                                }
                            }
                        }
                    }
                }
            }
        }

        private AccessorKind GetAccessorKind(SyntaxNode expression)
        {
            var syntaxNode = expression.Parent;
            if (syntaxNode == null)
            {
                return AccessorKind.Get;
            }
            var assignment = syntaxNode as AssignmentExpressionSyntax;
            if (assignment != null && assignment.Left == expression)
            {
                return syntaxNode.Kind() == SyntaxKind.SimpleAssignmentExpression ? AccessorKind.Set : AccessorKind.Both;
            }
            var kind = syntaxNode.Kind();
            if (kind == SyntaxKind.PostIncrementExpression ||
                kind == SyntaxKind.PostDecrementExpression ||
                kind == SyntaxKind.PreIncrementExpression ||
                kind == SyntaxKind.PreDecrementExpression)
            {
                return AccessorKind.Both;
            }

            if (syntaxNode is MemberAccessExpressionSyntax)
            {
                return GetAccessorKind(syntaxNode);
            }

            return AccessorKind.Get;
        }

        private void AddThrown(SyntaxNode thrown, SyntaxNode expression)
        {
            // find expressions of type throw new Object()
            var creationExpression = expression as ObjectCreationExpressionSyntax;
            if (creationExpression != null)
            {

                var thrownType = SemanticModel.GetTypeInfo(creationExpression).Type;
                if (!IsExceptionCaught(creationExpression, thrownType))
                {
                    Exceptions.Add(new UnhandledExceptionViolation(thrownType, thrown));
                }
                return;
            }

            // find expressions of type throw m_member or throw e and throw CallToMethod()
            if ((expression is IdentifierNameSyntax || expression is InvocationExpressionSyntax) && !string.IsNullOrEmpty(expression.ToString()))
            {
                var thrownType = SemanticModel.GetTypeInfo(expression).Type;
                if (!IsExceptionCaught(creationExpression, thrownType))
                {
                    Exceptions.Add(new UnhandledExceptionViolation(thrownType, expression));
                }
                return;
            }

            // rethrow with implicit type e.g. throw; find parent catch statement to see what we are throwing
            var parentCatchClauses = thrown.Ancestors().OfType<CatchClauseSyntax>();
            if (parentCatchClauses != null)
            {
                foreach (var catchClause in parentCatchClauses)
                {
                    var rethrownTypeSyntax = catchClause.Declaration?.Type;
                    // if specific exception is caught - that is what we are rethrowing (otherwise we will flag the exception in the original location where it is thrown)
                    if (rethrownTypeSyntax != null)
                    {
                        var rethrownType = SemanticModel.GetTypeInfo(rethrownTypeSyntax).Type;
                        if (rethrownType != null)
                        {
                            Exceptions.Add(new UnhandledExceptionViolation(rethrownType, thrown));
                        }
                    }
                }
            }
        }

        private bool IsExceptionCaughtInTryStatement(TryStatementSyntax tryStatement, ITypeSymbol thrownType)
        {
            foreach (var catchDeclaration in tryStatement.Catches)
            {
                // For filters e.g. "catch when (a==b)" or catch(Exception e) when (e is FileNotFoundException) we assume that we can't evaluate this at compile time, and the exception is not considered caught
                if (catchDeclaration.Filter != null)
                {
                    continue;
                }
                var typeCaughtSyntax = catchDeclaration.Declaration?.Type;

                // catch with no arguments - catches everything, unless there is a rethrow with no type specified
                if (typeCaughtSyntax == null)
                {
                    var rethrows = catchDeclaration.DescendantNodes().OfType<ThrowStatementSyntax>();
                    if (rethrows != null && rethrows.Any())
                    {
                        foreach (var rethrow in rethrows)
                        {
                            if (rethrow.Expression is IdentifierNameSyntax)
                            {
                                return true;
                            }
                        }
                    }
                    else
                    {
                        // catch with no rethrow
                        return true;
                    }
                }
                else
                {
                    // specific Exception is caught
                    var typeCaught = SemanticModel.GetTypeInfo(typeCaughtSyntax).Type;
                    if (typeCaught != null)
                    {
                        var conversion = Compilation.ClassifyConversion(thrownType, typeCaught);
                        if (conversion.IsImplicit)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool IsExceptionCaught<T>(T throwExpression, ITypeSymbol thrownType)
        {
            foreach (var tryStatement in BodySyntax.DescendantNodes().OfType<TryStatementSyntax>())
            {
                if (tryStatement.Block.DescendantNodes().OfType<T>().Contains(throwExpression))
                {
                    if (IsExceptionCaughtInTryStatement(tryStatement, thrownType))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

    }
}
