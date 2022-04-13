using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CheckedExceptions
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UnhandledExceptionsAnalyzer : BaseUnhandledExceptionsAnalyzer
    {
        // Unhandled Exceptions constants
        public const string UnhandledExceptionsDiagnosticId = "UnhandledExceptions";
        public static readonly string UnhandledExceptionsMessageFormat = "Unhandled exception(s): {0}";
        private static readonly string UnhandledExceptionsTitle = "Unhandled exception(s)";
        private static readonly string UnhandledExceptionsDescription = "All exceptions should be either caught or declared as thrown";
        private static readonly DiagnosticDescriptor UnhandledExceptionsRule = new DiagnosticDescriptor(
            UnhandledExceptionsDiagnosticId,
            UnhandledExceptionsTitle,
            UnhandledExceptionsMessageFormat,
            "Microsoft.Design",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: UnhandledExceptionsDescription);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(UnhandledExceptionsRule); } }

        protected override void Analyze(SyntaxNode root, SemanticModel semanticModel, SemanticModelAnalysisContext context)
        {
            // for each method / constructor
            foreach (var methodSyntax in root.DescendantNodes().OfType<BaseMethodDeclarationSyntax>())
            {
                ReportUnhandledExceptions(context, methodSyntax);
            }
        }

        /// <summary>
        /// Report exceptions generated within the method that are uncaught and undeclared
        /// </summary>
        /// <param name="context"></param>
        /// <param name="methodSyntax"></param>
        private void ReportUnhandledExceptions(SemanticModelAnalysisContext context, BaseMethodDeclarationSyntax methodSyntax)
        {
            // Get list of unhandled exceptions grouped by their location in the syntax tree
            var unhandledForMethod = GroupByLocation(GetUnhandledExceptions(context.SemanticModel, methodSyntax));

            // foreach location
            foreach (var violations in unhandledForMethod)
            {
                // create list of unhandled exceptions to pass into the Diagnostic
                var concatenatedExceptions = string.Join(",", violations.Value.Select(t => t.ExceptionType.ToString()));
                var prettyPrintConcatenatedExceptions = string.Join(", ", violations.Value.Select(t => t.ExceptionType.Name));
                var properties = GetDiagnosticProperties(context, methodSyntax, concatenatedExceptions);

                // Report a diagnostic with the list of unhandled exceptions generated in that location
                var diagnostic = Diagnostic.Create(
                    descriptor: UnhandledExceptionsRule,
                    location: violations.Key,
                    properties: properties,
                    messageArgs: (prettyPrintConcatenatedExceptions));
                context.ReportDiagnostic(diagnostic);
            }
        }

        /// <summary>
        /// Get exception and location pairs for every unhandled exception in a method
        /// </summary>
        /// <param name="semanticModel"></param>
        /// <param name="methodSyntax"></param>
        /// <returns></returns>
        private IEnumerable<UnhandledExceptionViolation> GetUnhandledExceptions(SemanticModel semanticModel, BaseMethodDeclarationSyntax methodSyntax)
        {
            // get exceptions declared in comments of this method
            var declaredExceptions = GetDeclaredExceptions(semanticModel.GetDeclaredSymbol(methodSyntax), false);

            // get exceptions thrown in the method
            var thrownExceptions = GetThrownExceptions(semanticModel, methodSyntax.Body != null ? (SyntaxNode)methodSyntax.Body : methodSyntax.ExpressionBody);

            // Filter out noisy exceptions
            var filteredExceptions = FilterExceptions(thrownExceptions);

            // unhandled = thrown - declared (including super types)
            var unhandledExceptions = filteredExceptions
                .Where(t => !declaredExceptions.Any(d => Compilation.ClassifyConversion(t.ExceptionType, d).IsImplicit));

            return unhandledExceptions;
        }
    }
}
