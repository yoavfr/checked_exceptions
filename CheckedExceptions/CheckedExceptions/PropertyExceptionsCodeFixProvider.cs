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
using Microsoft.CodeAnalysis.CSharp;

namespace CheckedExceptions
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PropertyExceptionsCodeFixProvider)), Shared]
    public class PropertyExceptionsCodeFixProvider : BaseCodeFixProvider
    {
        private const string HandleExceptionTitle = "Handle thrown exception(s)";
        private const string DeclareExceptionTitle = "Declare thrown exception(s)";
        private const string IgnoreExceptionTitle = "Ignore thrown exception(s)";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(PropertyExceptionsAnalyzer.GetterExceptionsDiagnosticId, PropertyExceptionsAnalyzer.SetterExceptionsDiagnosticId, PropertyExceptionsAnalyzer.ExpressionBodyExceptionsDiagnosticId);
            }
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var diagnosticSpan = diagnostic.Location.SourceSpan;
                AccessorKind accessorKind;
                switch (diagnostic.Id)
                {
                    case PropertyExceptionsAnalyzer.SetterExceptionsDiagnosticId:
                        accessorKind = AccessorKind.Set;
                        break;
                    case PropertyExceptionsAnalyzer.GetterExceptionsDiagnosticId:
                    case PropertyExceptionsAnalyzer.ExpressionBodyExceptionsDiagnosticId:
                        accessorKind = AccessorKind.Get;
                        break;
                    default:
                        accessorKind = AccessorKind.None;
                        break;
                }

                // Find a property declaration identified by the diagnostic.
                //
                var propertyDeclarationSyntax = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
                if (propertyDeclarationSyntax != null)
                {
                    var exceptions = diagnostic.Properties[BaseAnalyzer.ExceptionTypesProperty].Split(',');
                    var isShortComment = bool.Parse(diagnostic.Properties[BaseAnalyzer.IsPrivateMethodProperty]);
                    // Register a code action that will wrap the exception generating method with a try catch clause
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: HandleExceptionTitle,
                            createChangedSolution: c => AddTryCatch(context.Document, diagnostic.Location, exceptions, c),
                            equivalenceKey: HandleExceptionTitle),
                        diagnostic);

                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: DeclareExceptionTitle,
                            createChangedSolution: c => AddExceptionComment(
                                context.Document, 
                                propertyDeclarationSyntax, 
                                exceptions, 
                                c, 
                                false,
                                isShortComment,
                                accessorKind),
                            equivalenceKey: DeclareExceptionTitle),
                        diagnostic);

                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: IgnoreExceptionTitle,
                            createChangedSolution: c => AddExceptionComment(
                                context.Document, 
                                propertyDeclarationSyntax, 
                                exceptions, 
                                c, 
                                true,
                                isShortComment,
                                accessorKind),
                            equivalenceKey: IgnoreExceptionTitle),
                        diagnostic);
                }
            }
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
