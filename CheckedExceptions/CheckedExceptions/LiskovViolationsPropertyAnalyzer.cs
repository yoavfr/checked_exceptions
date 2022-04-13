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
    public class LiskovViolationsPropertyAnalyzer : BaseAnalyzer
    {
        // Base method  violation constants
        public const string BasePropertyGetterViolationsDiagnosticId = "BasePropertyGetterViolations";
        private static readonly string BasePropertyGetterViolationsTitle = "Exception(s) thrown in getter but not declared in base property";
        public static readonly string BasePropertyGetterViolationsMessageFormat = "Property {0} getter throws exception(s) {1} not declared in base method {2}.{0}";
        private static readonly string BasePropertyGetterViolationsDescription = "Implementation should not throw exceptions that are not declared in property's base getter";

        private static DiagnosticDescriptor BasePropertyGetterViolationsRule = new DiagnosticDescriptor(
            BasePropertyGetterViolationsDiagnosticId,
            BasePropertyGetterViolationsTitle,
            BasePropertyGetterViolationsMessageFormat, 
            "Microsoft.Design", 
            DiagnosticSeverity.Warning, 
            isEnabledByDefault: true, 
            description: BasePropertyGetterViolationsDescription);

        // Interface violation constants
        public const string BasePropertySetterViolationsDiagnosticId = "BasePropertySetterViolations";
        private static readonly string BasePropertySetterViolationsTitle = "Exception(s) thrown in setter but not declared in base property";
        public static readonly string BasePropertySetterViolationsMessageFormat = "Property {0} setter throws exception(s) {1} not declared in base method {2}.{0}";
        private static readonly string BasePropertySetterViolationsDescription = "Implementation should not throw exceptions that are not declared in property's base setter";

        private static DiagnosticDescriptor BasePropertySetterViolationsRule = new DiagnosticDescriptor(
            BasePropertySetterViolationsDiagnosticId,
            BasePropertySetterViolationsTitle,
            BasePropertySetterViolationsMessageFormat,
            "Microsoft.Design",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: BasePropertySetterViolationsDescription);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(BasePropertyGetterViolationsRule, BasePropertySetterViolationsRule); } }

        protected override void Analyze(SyntaxNode root, SemanticModel semanticModel, SemanticModelAnalysisContext context)
        {
            foreach (var propertyDeclarationSyntax in root.DescendantNodes().OfType<PropertyDeclarationSyntax>())
            {
                ReportLiskovSubstitutionViolations(context, propertyDeclarationSyntax, AccessorKind.Get, BasePropertyGetterViolationsRule);
                ReportLiskovSubstitutionViolations(context, propertyDeclarationSyntax, AccessorKind.Set, BasePropertySetterViolationsRule);
            }
        }

        /// <summary>
        /// ReportLiskovSubstitutionViolations
        /// </summary>
        /// <param name="context"></param>
        /// <param name="methodSyntax"></param>
        /// <exception cref="InvalidOperationException">Ignore.</exception>
        private void ReportLiskovSubstitutionViolations(SemanticModelAnalysisContext context, MemberDeclarationSyntax methodSyntax, AccessorKind accessorKind, DiagnosticDescriptor rule)
        {
            var violations = GetBaseMethodViolations(context, methodSyntax, accessorKind)
                .Union(GetInterfaceViolations(context, methodSyntax, accessorKind));

            var undelcaredInBase = GroupByLocation(violations);

            foreach (var violation in undelcaredInBase)
            {
                // create list of unhandled exceptions to pass into the Diagnostic
                var concatenatedExceptions = string.Join(",", violation.Value.Select(t => t.ExceptionType.ToString()));
                var prettyPrintConcatenatedExceptions = string.Join(", ", violation.Value.Select(t => t.ExceptionType.Name));
                var properties = GetDiagnosticProperties(context, methodSyntax, concatenatedExceptions);

                var diagnostic = Diagnostic.Create(
                    descriptor: rule,
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
        /// <param name="accessorKind"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Ignore.</exception>
        /// <exception cref="OverflowException">Ignore.</exception>
        private HashSet<BaseMethodViolation> GetInterfaceViolations(SemanticModelAnalysisContext context, MemberDeclarationSyntax methodSyntax, AccessorKind accessorKind)
        {
            var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodSyntax);
            var interfaceViolations = new HashSet<BaseMethodViolation>();

            // Find interface declarations of this method 
            var methodDeclarationsInInterface = methodSymbol.ContainingType.AllInterfaces.SelectMany(i => i.GetMembers().OfType<IPropertySymbol>()).Where(
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
            var exceptionsDeclaredForMethod = GetDeclaredExceptions(context.SemanticModel.GetDeclaredSymbol(methodSyntax), true, accessorKind);

            // Find exceptions declared in method but not in interface
            foreach (var methodDeclarationInInterface in methodDeclarationsInInterface)
            {
                // If the no setter or getter are defined in the interface - then we can't violate LSP
                if (accessorKind == AccessorKind.Set && methodDeclarationInInterface.SetMethod == null ||
                    accessorKind == AccessorKind.Get && methodDeclarationInInterface.GetMethod == null)
                {
                    continue;
                }

                var exceptionsDeclaredForInterface = GetDeclaredExceptions(methodDeclarationInInterface, true, accessorKind);
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
        protected HashSet<BaseMethodViolation> GetBaseMethodViolations(SemanticModelAnalysisContext context, MemberDeclarationSyntax methodSyntax, AccessorKind accessorKind)
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
            var baseSymbol = baseType.GetMembers().OfType<IPropertySymbol>().FirstOrDefault(p => p.Name == methodSymbol.Name);

            // No matching declaration in base
            if (baseSymbol == null ||
                accessorKind == AccessorKind.Set && baseSymbol.SetMethod == null ||
                accessorKind == AccessorKind.Get && baseSymbol.GetMethod == null)
            {
                return baseMethodViolations;
            }

            // Exceptions declared for this method
            var exceptionsDeclaredForMethod = GetDeclaredExceptions(context.SemanticModel.GetDeclaredSymbol(methodSyntax), true, accessorKind);

            var exceptionsDeclaredForBase = GetDeclaredExceptions(baseSymbol, true, accessorKind);
            var notDeclaredInBase = exceptionsDeclaredForMethod.Except(exceptionsDeclaredForBase);
            foreach (var exceptionType in notDeclaredInBase)
            {
                baseMethodViolations.Add(new BaseMethodViolation(exceptionType, baseSymbol.ContainingType, methodSymbol, methodSyntax.ChildTokens().Last().GetLocation()));
            }
            return baseMethodViolations;
        }

    }
}