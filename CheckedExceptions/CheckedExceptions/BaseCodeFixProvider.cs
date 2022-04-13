using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace CheckedExceptions
{
    public abstract class BaseCodeFixProvider : CodeFixProvider
    {
        protected async Task<Solution> AddExceptionComment(Document document, SyntaxNode syntaxNode, IEnumerable<string> exceptionNames, CancellationToken cancellationToken, bool ignore, bool shortComment, AccessorKind accessorKind = AccessorKind.None)
        {
            var rewriter = new DocumentationCommentRewriter(document, syntaxNode, exceptionNames, ignore, accessorKind, shortComment);

            var newSyntaxRoot = rewriter.Visit(await document.GetSyntaxRootAsync().ConfigureAwait(false));
            var newSolution = document.Project.Solution.WithDocumentSyntaxRoot(document.Id, newSyntaxRoot);

            return newSolution;
        }

        protected async Task<Solution> AddShortExceptionComment(Document document, SyntaxNode syntaxNode, IEnumerable<string> exceptionNames, CancellationToken cancellationToken, bool ignore, bool shortComment, AccessorKind accessorKind = AccessorKind.None)
        {
            var rewriter = new DocumentationCommentRewriter(document, syntaxNode, exceptionNames, ignore, accessorKind, shortComment);

            var newSyntaxRoot = rewriter.Visit(await document.GetSyntaxRootAsync().ConfigureAwait(false));
            var newSolution = document.Project.Solution.WithDocumentSyntaxRoot(document.Id, newSyntaxRoot);

            return newSolution;
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }
    }
}
