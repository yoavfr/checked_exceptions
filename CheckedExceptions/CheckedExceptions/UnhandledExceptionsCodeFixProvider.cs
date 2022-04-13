using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace CheckedExceptions
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnhandledExceptionsCodeFixProvider)), Shared]
    public class UnhandledExceptionsCodeFixProvider : BaseCodeFixProvider
    {
        private const string DeclareExceptionTitle = "Declare thrown exception(s)";
        private const string IgnoreExceptionTitle = "Ignore thrown exception(s)";
        private const string HandleExceptionTitle = "Handle thrown exception(s)";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(UnhandledExceptionsAnalyzer.UnhandledExceptionsDiagnosticId);
            }
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var diagnosticSpan = diagnostic.Location.SourceSpan;

                // Find a method declaration identified by the diagnostic.
                //
                var methodDeclarationSyntax = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
                if (methodDeclarationSyntax != null)
                {
                    RegisterCodeFixesAsync(context, diagnostic, methodDeclarationSyntax);
                    return;
                }

                // Find a constructor identified by the diagnostic
                //
                var constructorDeclarationSyntax = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ConstructorDeclarationSyntax>().FirstOrDefault();
                if (constructorDeclarationSyntax != null)
                {
                    RegisterCodeFixesAsync(context, diagnostic, constructorDeclarationSyntax);
                    return;
                }
            }
        }

        private void RegisterCodeFixesAsync(CodeFixContext context, Diagnostic diagnostic, BaseMethodDeclarationSyntax targetMethodDeclarationSyntax)
        {

            // Register a code action that will declare the exception.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: DeclareExceptionTitle,
                    createChangedSolution: c => AddExceptionComment(
                        context.Document, 
                        targetMethodDeclarationSyntax, 
                        diagnostic.Properties[BaseAnalyzer.ExceptionTypesProperty].Split(','), 
                        c, 
                        false,
                        bool.Parse(diagnostic.Properties[BaseAnalyzer.IsPrivateMethodProperty])),
                    equivalenceKey: DeclareExceptionTitle),
                diagnostic);

            // Register a code action that will wrap the exception generating method with a try catch clause
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: HandleExceptionTitle,
                    createChangedSolution: c => AddTryCatch(context.Document, diagnostic.Location, diagnostic.Properties[BaseAnalyzer.ExceptionTypesProperty].Split(','), c),
                    equivalenceKey: HandleExceptionTitle),
                diagnostic);

            // Register a code action that will declare the exception and ignore it upstream.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: IgnoreExceptionTitle,
                    createChangedSolution: c => AddExceptionComment(
                        context.Document, 
                        targetMethodDeclarationSyntax, 
                        diagnostic.Properties[BaseAnalyzer.ExceptionTypesProperty].Split(','), 
                        c, 
                        true,
                        bool.Parse(diagnostic.Properties[BaseAnalyzer.IsPrivateMethodProperty])),
                    equivalenceKey: IgnoreExceptionTitle),
                diagnostic);
        }

        private async Task<Solution> AddTryCatch(Document document, Location location, IEnumerable<string> exceptionNames, CancellationToken cancellationToken)
        {
            var rewriter = new HandleExceptionRewriter(document, location, exceptionNames);

            var newSyntaxRoot = Formatter.Format(rewriter.Visit(await document.GetSyntaxRootAsync().ConfigureAwait(false)), document.Project.Solution.Workspace);
            var newSolution = document.Project.Solution.WithDocumentSyntaxRoot(document.Id, newSyntaxRoot);

            return newSolution;
        }
    }

}
