using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace CheckedExceptions
{
    class HandleExceptionRewriter : BaseRewriter
    {
        private readonly Location m_Location;
        private readonly IEnumerable<string> m_ExceptionNames;
        private bool m_Done;

        public HandleExceptionRewriter (Document document, Location location, IEnumerable<string> exceptionNames) : base (document)
        {
            m_Location = location;
            m_ExceptionNames = exceptionNames;
        }

        public override SyntaxNode VisitTryStatement(TryStatementSyntax node)
        {
            var nodeAtLocation = node.DescendantNodes().FirstOrDefault(n => n.GetLocation() == m_Location);
            
            // for re-throws - wrap the rethrow instead of adding the same exception to the enclosing try catch
            if (nodeAtLocation != null && nodeAtLocation.Parent != null && !(nodeAtLocation.Parent.Parent is CatchClauseSyntax))
            {
                var catchClauses = node.Catches;
                foreach (var exception in m_ExceptionNames)
                {
                    catchClauses = catchClauses.Add(SyntaxFactory.CatchClause().WithDeclaration(SyntaxFactory.CatchDeclaration(SyntaxFactory.ParseTypeName(ShortExceptionName(exception)))));
                }
                m_Done = true;
                return node.WithCatches(catchClauses);
            }
            return base.VisitTryStatement(node);
        }

        public override SyntaxNode Visit(SyntaxNode node)
        {
            if (!m_Done && node != null && node.GetLocation() == m_Location)
            {
                var statementSyntax = node as StatementSyntax;
                if (statementSyntax != null)
                {
                    var catchClauses = new SyntaxList<CatchClauseSyntax>();
                    foreach (var exception in m_ExceptionNames)
                    {
                        catchClauses = catchClauses.Add(SyntaxFactory.CatchClause().WithDeclaration(SyntaxFactory.CatchDeclaration(SyntaxFactory.ParseTypeName(ShortExceptionName(exception)))));
                    }

                    var tryStatement = SyntaxFactory.TryStatement(
                        SyntaxFactory.Block(new[] { statementSyntax }), catchClauses, null);
                    return tryStatement;
                }
            }
            return base.Visit(node);
        }
    }
}
