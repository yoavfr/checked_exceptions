using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace CheckedExceptions
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LiskovViolationsCodeFixProvider)), Shared]
    public class LiskovViolationsCodeFixProvider : BaseCodeFixProvider
    {
        private const string IgnoreExceptionTitle = "Ignore thrown exception(s)";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(
              LiskovViolationsMemberAnalyzer.InterfaceViolationsDiagnosticId,
              LiskovViolationsMemberAnalyzer.BaseMethodViolationsDiagnosticId,
              LiskovViolationsPropertyAnalyzer.BasePropertyGetterViolationsDiagnosticId,
              LiskovViolationsPropertyAnalyzer.BasePropertySetterViolationsDiagnosticId);
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
                switch (diagnostic.Id)
                {
                    case LiskovViolationsMemberAnalyzer.InterfaceViolationsDiagnosticId:
                    case LiskovViolationsMemberAnalyzer.BaseMethodViolationsDiagnosticId:
                        var methodDeclarationSyntax = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
                        if (methodDeclarationSyntax != null)
                        {
                            RegisterCodeFixesAsync(context, diagnostic, methodDeclarationSyntax);
                        }
                        break;
                    case LiskovViolationsPropertyAnalyzer.BasePropertyGetterViolationsDiagnosticId:
                        var getterDeclarationSyntax = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
                        if (getterDeclarationSyntax != null)
                        {
                            RegisterCodeFixesAsync(context, diagnostic, getterDeclarationSyntax, AccessorKind.Get);
                        }
                        break;
                    case LiskovViolationsPropertyAnalyzer.BasePropertySetterViolationsDiagnosticId:
                        var setterDeclarationSyntax = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
                        if (setterDeclarationSyntax != null)
                        {
                            RegisterCodeFixesAsync(context, diagnostic, setterDeclarationSyntax, AccessorKind.Set);
                        }
                        break;
                }
            }
        }

        private void RegisterCodeFixesAsync(CodeFixContext context, Diagnostic diagnostic, SyntaxNode targetMethodDeclarationSyntax, AccessorKind accessorKind = AccessorKind.None)
        {
            // Register a code action that will declare the exception and ignore it upstream.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: IgnoreExceptionTitle,
                    createChangedSolution: c => AddExceptionComment(context.Document, targetMethodDeclarationSyntax, diagnostic.Properties[BaseAnalyzer.ExceptionTypesProperty].Split(','), c, true, false, accessorKind),
                    equivalenceKey: IgnoreExceptionTitle),
                diagnostic);
        }
    }
}
