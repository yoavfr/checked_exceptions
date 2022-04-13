using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace CheckedExceptions
{
    class DocumentationCommentRewriter : BaseRewriter
    {
        private readonly SyntaxNode m_TargetMethod;
        private readonly IEnumerable<string> m_ExceptionNames;
        private readonly bool m_Ignore;
        private readonly AccessorKind m_AccessorKind;
        private readonly bool m_ShortComment;

        public DocumentationCommentRewriter(Document document, SyntaxNode targetMethod, IEnumerable<string> exceptionNames, bool ignore, AccessorKind accessorKind, bool shortComment) : base(document)
        {
            m_TargetMethod = targetMethod;
            m_ExceptionNames = exceptionNames;
            m_Ignore = ignore;
            m_AccessorKind = accessorKind;
            m_ShortComment = shortComment;
        }

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            if (node != m_TargetMethod)
            {
                return base.VisitConstructorDeclaration(node);
            }

            var exceptions = m_ExceptionNames.Select(e => new ExceptionDeclaration(e, ShortExceptionName(e), null, m_Ignore, AccessorKind.None));
            var parameters = node.ParameterList.Parameters.Select(param => param.Identifier.Text).Select(p => new ParameterDeclaration(p, string.Empty));
            var commentBuilder = new XmlCommentBuilder(
                node,
                exceptions,
                m_ShortComment,
                "ctor",
                null,
                parameters);

            return node.WithLeadingTrivia(commentBuilder.ToSyntaxTrivia());
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (node != m_TargetMethod)
            {
                return base.VisitMethodDeclaration(node);
            }

            var exceptions = m_ExceptionNames.Select(e => new ExceptionDeclaration(e, ShortExceptionName(e), null, m_Ignore, AccessorKind.None));
            var parameters = node.ParameterList.Parameters.Select(param => param.Identifier.Text).Select(p => new ParameterDeclaration(p, string.Empty));
            var commentBuilder = new XmlCommentBuilder(
                node,
                exceptions,
                m_ShortComment,
                node.Identifier.ToString(),
                node.ReturnType.ToString(),
                parameters);
           
            return node.WithLeadingTrivia(commentBuilder.ToSyntaxTrivia());
        }

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            if (node != m_TargetMethod)
            {
                return base.VisitPropertyDeclaration(node);
            }

            var exceptions = m_ExceptionNames.Select(e => new ExceptionDeclaration(e, ShortExceptionName(e), null, m_Ignore, m_AccessorKind));
            var commentBuilder = new XmlCommentBuilder(
                node,
                exceptions,
                m_ShortComment,
                node.Identifier.ToString());

            return node.WithLeadingTrivia(commentBuilder.ToSyntaxTrivia());
        }
    }
}
