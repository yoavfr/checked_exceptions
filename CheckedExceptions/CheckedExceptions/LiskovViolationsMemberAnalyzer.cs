using System;
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
    public class LiskovViolationsMemberAnalyzer : BaseAnalyzer
    {
        // Base method  violation constants
        public const string BaseMethodViolationsDiagnosticId = "BaseMethodViolations";
        private static readonly string BaseMethodViolationsTitle = "Exception(s) thrown but not declared in base method";
        public static readonly string BaseMethodViolationsMessageFormat = "Method {0} throws exception(s) {1} not declared in base method {2}.{0}";
        private static readonly string BaseMethodViolationsDescription = "Implementation should not throw exceptions that are not declared in base method";

        private static DiagnosticDescriptor BaseMethodViolationsRule = new DiagnosticDescriptor(
            BaseMethodViolationsDiagnosticId, 
            BaseMethodViolationsTitle, 
            BaseMethodViolationsMessageFormat, 
            "Microsoft.Design", 
            DiagnosticSeverity.Warning, 
            isEnabledByDefault: true, 
            description: BaseMethodViolationsDescription);

        // Interface violation constants
        public const string InterfaceViolationsDiagnosticId = "InterfaceViolations";
        private static readonly string InterfaceViolationsTitle = "Exception(s) thrown but not declared in interface";
        public static readonly string InterfaceViolationsMessageFormat = "Method {0} throws exception(s) {1} not declared in interface {2}.{0}";
        private static readonly string InterfaceViolationsDescription = "Implementation should not throw exceptions that are not declared in interface";

        private static DiagnosticDescriptor InterfaceViolationsRule = new DiagnosticDescriptor(
            InterfaceViolationsDiagnosticId, 
            InterfaceViolationsTitle, 
            InterfaceViolationsMessageFormat, 
            "Microsoft.Design", 
            DiagnosticSeverity.Warning, 
            isEnabledByDefault: true, 
            description: InterfaceViolationsDescription);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(BaseMethodViolationsRule, InterfaceViolationsRule); } }

        protected override void Analyze(SyntaxNode root, SemanticModel semanticModel, SemanticModelAnalysisContext context)
        {
            // for each method / constructor
            foreach (var methodSyntax in root.DescendantNodes().OfType<BaseMethodDeclarationSyntax>())
            {
                ReportLiskovSubstitutionViolations(context, methodSyntax);
            }
        }
        
        /// <summary>
        /// ReportLiskovSubstitutionViolations
        /// </summary>
        /// <param name="context"></param>
        /// <param name="methodSyntax"></param>
        /// <exception cref="InvalidOperationException">Ignore.</exception>
        private void ReportLiskovSubstitutionViolations(SemanticModelAnalysisContext context, MemberDeclarationSyntax methodSyntax)
        {
            var undelcaredInBase = GroupByLocation(FilterExceptions(GetBaseMethodViolations(context, methodSyntax)));

            foreach (var violation in undelcaredInBase)
            {
                // create list of unhandled exceptions to pass into the Diagnostic
                var concatenatedExceptions = string.Join(",", violation.Value.Select(t => t.ExceptionType.ToString()));
                var prettyPrintConcatenatedExceptions = string.Join(", ", violation.Value.Select(t => t.ExceptionType.Name));
                var properties = GetDiagnosticProperties(context, methodSyntax, concatenatedExceptions);

                var diagnostic = Diagnostic.Create(
                    descriptor: BaseMethodViolationsRule,
                    location: violation.Key,
                    properties: properties,
                    messageArgs: new object[] { violation.Value.First().Method.Name, prettyPrintConcatenatedExceptions, violation.Value.First().Interface.Name });
                context.ReportDiagnostic(diagnostic);
            }

            var interfaceViolations = GroupByLocation(FilterExceptions(GetInterfaceViolations(context, methodSyntax)));

            foreach (var violation in interfaceViolations)
            {
                var concatenatedExceptions = string.Join(",", violation.Value.Select(t => t.ExceptionType.ToString()));
                var prettyPrintConcatenatedExceptions = string.Join(", ", violation.Value.Select(t => t.ExceptionType.Name));
                var properties = GetDiagnosticProperties(context, methodSyntax, concatenatedExceptions);

                var diagnostic = Diagnostic.Create(
                    descriptor: InterfaceViolationsRule,
                    location: violation.Key,
                    properties: properties,
                    messageArgs: new object[] { violation.Value.First().Method.Name, prettyPrintConcatenatedExceptions, violation.Value.First().Interface.Name });
                context.ReportDiagnostic(diagnostic);
            }
        }

        /// <summary>
        /// GetInterfaceViolations
        /// </summary>
        /// <param name="context"></param>
        /// <param name="methodSyntax"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Ignore.</exception>
        /// <exception cref="OverflowException">Ignore.</exception>
        private HashSet<BaseMethodViolation> GetInterfaceViolations(SemanticModelAnalysisContext context, MemberDeclarationSyntax methodSyntax)
        {
            var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodSyntax);
            var interfaceViolations = new HashSet<BaseMethodViolation>();

            // Find interface declarations of this method 
            var methodDeclarationsInInterface = methodSymbol.ContainingType.AllInterfaces.SelectMany(i => i.GetMembers().OfType<IMethodSymbol>()).Where(
                (interfaceMethod) =>
                {
                    var implementation = methodSymbol.ContainingType.FindImplementationForInterfaceMember(interfaceMethod);
                    return implementation != null && implementation.Equals(methodSymbol);
                });
            if (!methodDeclarationsInInterface.Any())
            {
                return interfaceViolations;
            }

            // Get exceptions declared for this method
            var exceptionsDeclaredForMethod = GetDeclaredExceptions(context.SemanticModel.GetDeclaredSymbol(methodSyntax), true);

            // Find exceptions declared in method but not in interface
            foreach (var methodDeclarationInInterface in methodDeclarationsInInterface)
            {
                var exceptionsDeclaredForInterface = GetDeclaredExceptions(methodDeclarationInInterface, true);
                var notDeclaredInInterface = exceptionsDeclaredForMethod.Except(exceptionsDeclaredForInterface);
                foreach (var exceptionType in notDeclaredInInterface)
                {
                    interfaceViolations.Add(new BaseMethodViolation(exceptionType, methodDeclarationInInterface.ContainingType, methodSymbol, methodSyntax.ChildTokens().Last().GetLocation()));
                }
            }
            return interfaceViolations;
        }

        /// <summary>
        /// GetBaseMethodViolations
        /// </summary>
        /// <param name="context"></param>
        /// <param name="methodSyntax"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Ignore.</exception>
        private HashSet<BaseMethodViolation> GetBaseMethodViolations(SemanticModelAnalysisContext context, MemberDeclarationSyntax methodSyntax)
        {
            var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodSyntax);
            var baseMethodViolations = new HashSet<BaseMethodViolation>();

            // Skip constructors - doesn't make sense to flag these
            if (methodSymbol.ContainingType.Constructors.Contains(methodSymbol))
            {
                return baseMethodViolations;
            }
            var baseType = methodSymbol.ContainingType.BaseType;

            // No base type
            if (baseType == null)
            {
                return baseMethodViolations;
            }

            // Mehtod or constructor
            var baseSymbol = baseType.GetMembers().OfType<IMethodSymbol>().FirstOrDefault(m => m.Name == methodSymbol.Name);
            
            // No matching methods in base type
            if (baseSymbol == null)
            {
                return baseMethodViolations;
            }

            // Exceptions declared for this method
            var exceptionsDeclaredForMethod = GetDeclaredExceptions(context.SemanticModel.GetDeclaredSymbol(methodSyntax), true);

            var exceptionsDeclaredForBase = GetDeclaredExceptions(baseSymbol, true);
            var notDeclaredInBase = exceptionsDeclaredForMethod.Except(exceptionsDeclaredForBase);
            foreach (var exceptionType in notDeclaredInBase)
            {
                baseMethodViolations.Add(new BaseMethodViolation(exceptionType, baseSymbol.ContainingType, methodSymbol, methodSyntax.ChildTokens().Last().GetLocation()));
            }
            return baseMethodViolations;
        }

    }
}