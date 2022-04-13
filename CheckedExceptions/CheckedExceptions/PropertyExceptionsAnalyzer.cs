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
    public class PropertyExceptionsAnalyzer : BaseUnhandledExceptionsAnalyzer
    {
        // Rule for Get property accessors
        public const string GetterExceptionsDiagnosticId = "ExceptionThrownInGetter";
        public static readonly string GetterExceptionsMessageFormat = "Unhandled exception(s): {0}";
        private static readonly string GetterExceptionsTitle = "Unhandled exception(s)";
        private static readonly string GetterExceptionsDescription = "Generally it is not wise to throw exceptions from a property getter.";
        private static readonly DiagnosticDescriptor GetterExceptionsRule = new DiagnosticDescriptor(
            GetterExceptionsDiagnosticId,
            GetterExceptionsTitle,
            GetterExceptionsMessageFormat,
            "Microsoft.Design",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: GetterExceptionsDescription);

        // Rule for Expression body get accessor
        public const string ExpressionBodyExceptionsDiagnosticId = "ExceptionThrownInPropertyExpressionBody";
        public static readonly string ExpressionBodyExceptionsMessageFormat = "Unhandled exception(s): {0}";
        private static readonly string ExpressionBodyExceptionsTitle = "Unhandled exception(s)";
        private static readonly string ExpressionBodyExceptionsDescription = "Generally it is not wise to throw exceptions from a property getter.";
        private static readonly DiagnosticDescriptor ExpressionBodyExceptionsRule = new DiagnosticDescriptor(
            ExpressionBodyExceptionsDiagnosticId,
            ExpressionBodyExceptionsTitle,
            ExpressionBodyExceptionsMessageFormat,
            "Microsoft.Design",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: ExpressionBodyExceptionsDescription);

        // Rule for Set property accessors
        public const string SetterExceptionsDiagnosticId = "ExceptionThrownInSetter";
        public static readonly string SetterExceptionsMessageFormat = "Unhandled exception(s): {0}";
        private static readonly string SetterExceptionsTitle = "Unhandled exception(s)";
        private static readonly string SetterExceptionsDescription = "All exceptions should be either caught or declared as thrown";
        private static readonly DiagnosticDescriptor SetterExceptionsRule = new DiagnosticDescriptor(
            SetterExceptionsDiagnosticId,
            SetterExceptionsTitle,
            SetterExceptionsMessageFormat,
            "Microsoft.Design",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: SetterExceptionsDescription);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(GetterExceptionsRule, SetterExceptionsRule, ExpressionBodyExceptionsRule); } }

        protected override void Analyze(SyntaxNode root, SemanticModel semanticModel, SemanticModelAnalysisContext context)
        {
            // for each getter/setter accessor
            foreach (var accessorSyntax in root.DescendantNodes().OfType<AccessorDeclarationSyntax>())
            {
                ReportUnhandledExceptions(context, accessorSyntax);
            }

            // for each property with arrow clause e.g. int Foo => 1;
            foreach (var propertyDeclaration in root.DescendantNodes().OfType<PropertyDeclarationSyntax>())
            {
                foreach (var arrowExpression in propertyDeclaration.DescendantNodes().OfType<ArrowExpressionClauseSyntax>())
                {
                    ReportUnhandledExceptions(context, arrowExpression);
                }
            }
        }

        /// <summary>
        /// Report exceptions generated within the getter that are uncaught
        /// </summary>
        /// <param name="context"></param>
        /// <param name="accessorSyntax"></param>
        private void ReportUnhandledExceptions(SemanticModelAnalysisContext context, AccessorDeclarationSyntax accessorSyntax)
        {
            // Get list of unhandled exceptions grouped by their location in the syntax tree
            var accessorKind = accessorSyntax.Kind() == SyntaxKind.GetAccessorDeclaration ? AccessorKind.Get : AccessorKind.Set;
            var unhandled = GroupByLocation(GetUnhandledExceptions(context.SemanticModel,
                accessorSyntax.Body != null ? (SyntaxNode)accessorSyntax.Body : accessorSyntax.ExpressionBody, accessorKind));

            // foreach location
            foreach (var violations in unhandled)
            {
                // create list of unhandled exceptions to pass into the Diagnostic
                var concatenatedExceptions = string.Join(",", violations.Value.Select(t => t.ExceptionType.ToString()));
                var prettyPrintConcatenatedExceptions = string.Join(", ", violations.Value.Select(t => t.ExceptionType.Name));
                var properties = GetDiagnosticProperties(context, accessorSyntax, concatenatedExceptions);

                // Report a diagnostic with the list of unhandled exceptions generated in that location
                var diagnostic = Diagnostic.Create(
                    descriptor: accessorSyntax.Kind() == SyntaxKind.GetAccessorDeclaration ? GetterExceptionsRule : SetterExceptionsRule,
                    location: violations.Key,
                    properties: properties,
                    messageArgs: (prettyPrintConcatenatedExceptions));
                context.ReportDiagnostic(diagnostic);
            }
        }

        private void ReportUnhandledExceptions(SemanticModelAnalysisContext context, ArrowExpressionClauseSyntax arrowExpressionSyntax)
        {
            var unhandledForExpression = GroupByLocation(GetUnhandledExceptions(context.SemanticModel, arrowExpressionSyntax, AccessorKind.Get));

            // foreach location
            foreach (var violations in unhandledForExpression)
            {
                // create list of unhandled exceptions to pass into the Diagnostic
                var concatenatedExceptions = string.Join(",", violations.Value.Select(t => t.ExceptionType.ToString()));
                    var prettyPrintConcatenatedExceptions = string.Join(", ", violations.Value.Select(t => t.ExceptionType.Name));
                var properties = GetDiagnosticProperties(context, arrowExpressionSyntax, concatenatedExceptions);

                // Report a diagnostic with the list of unhandled exceptions generated in that location
                var diagnostic = Diagnostic.Create(
                        descriptor: ExpressionBodyExceptionsRule,
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
        /// <param name="accessorSyntax"></param>
        /// <returns></returns>
        private IEnumerable<UnhandledExceptionViolation> GetUnhandledExceptions(SemanticModel semanticModel, SyntaxNode syntaxNode, AccessorKind accessorKind)
        {
            // get exceptions declared in comments of this method
            if (syntaxNode != null)
            {
                var propertyDeclarationSyntax = syntaxNode.AncestorsAndSelf().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
                if (propertyDeclarationSyntax != null)
                {
                    var propertyDeclaration = semanticModel.GetDeclaredSymbol(propertyDeclarationSyntax);
                    if (propertyDeclaration != null)
                    {
                        var declaredExceptions = GetDeclaredExceptions(propertyDeclaration, false, accessorKind);

                        // get exceptions thrown in the method
                        var thrownExceptions = GetThrownExceptions(semanticModel, syntaxNode);

                        // Filter out noisy exceptions
                        var filteredExceptions = FilterExceptions(thrownExceptions);

                        // unhandled = thrown - declared (including super types)
                        var unhandledExceptions = filteredExceptions
                            .Where(t => !declaredExceptions.Any(d => Compilation.ClassifyConversion(t.ExceptionType, d).IsImplicit));

                        return unhandledExceptions;
                    }
                }
            }
            return new UnhandledExceptionViolation[0];
        }

        private ImmutableDictionary<string, string> GetDiagnosticProperties(SemanticModelAnalysisContext context, AccessorDeclarationSyntax accessorDeclarationSyntax, string concatenatedExceptions)
        {
            var propertyDeclarationsyntax = accessorDeclarationSyntax.Ancestors().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
            return GetDiagnosticProperties(context, propertyDeclarationsyntax, concatenatedExceptions);
        }

        private ImmutableDictionary<string, string> GetDiagnosticProperties(SemanticModelAnalysisContext context, ArrowExpressionClauseSyntax arrowExpressionClauseSyntax, string concatenatedExceptions)
        {
            var propertyDeclarationsyntax = arrowExpressionClauseSyntax.Ancestors().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
            return GetDiagnosticProperties(context, propertyDeclarationsyntax, concatenatedExceptions);
        }
    }
}
