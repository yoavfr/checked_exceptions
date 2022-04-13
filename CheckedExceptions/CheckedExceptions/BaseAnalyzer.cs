using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace CheckedExceptions
{
    public abstract class BaseAnalyzer : DiagnosticAnalyzer
    {
        protected Compilation Compilation { get; set; }

        public const string ExceptionTypesProperty = "ExceptionTypes";
        public const string IsPrivateMethodProperty = "IsPrivateMethod";

        #pragma warning disable RS1012 // Start action has no registered actions.
        private void CompilationStartAction(CompilationStartAnalysisContext obj)
        #pragma warning restore RS1012 // Start action has no registered actions.
        {
            Compilation = obj.Compilation;
        }

        private void AnalyzeSemanticAction(SemanticModelAnalysisContext context)
        {
            if (Compilation == null)
            {
                return;
            }
            var semanticModel = context.SemanticModel;
            var root = semanticModel.SyntaxTree.GetRoot();

            // Ignore auto-generated Designer.cs files. Unfortunately our comments here will be lost anyhow
            var fileName = Path.GetFileName(root.SyntaxTree.FilePath);
            if (!string.IsNullOrEmpty(fileName) && fileName.EndsWith(".Designer.cs"))
            {
                return;
            }

            Analyze(root, semanticModel, context);
        }

        protected abstract void Analyze(SyntaxNode root, SemanticModel semanticModel, SemanticModelAnalysisContext context);
        
        /// <summary>
        /// Group Exceptions by location in syntax tree
        /// </summary>
        /// <param name="violations"></param>
        /// <returns></returns>
        protected Dictionary<Location, List<T>> GroupByLocation<T>(IEnumerable<T> violations) where T : Violation
        {
            var result = new Dictionary<Location, List<T>>();
            foreach (var violation in violations)
            {
                var location = violation.Location;
                if (!result.ContainsKey(location))
                {
                    result.Add(location, new List<T>());
                }
                result[location].Add(violation);
            }
            return result;
        }

        /// <summary>
        /// Filter out noisy exceptions that it is generally OK to ignore (equivalent to Java RuntimeException descendants)
        /// </summary>
        /// <param name="exceptions"></param>
        /// <returns></returns>
        protected IEnumerable<T> FilterExceptions<T>(HashSet<T> exceptions) where T : Violation
        {
            var exceptionsToFilter = new List<ITypeSymbol>();

            if (!ExceptionFilters.Instance.FlagArgumentExceptions)
            {
                var excluded = Compilation.GetTypeByMetadataName("System.ArgumentException");
                if (excluded != null)
                {
                    exceptionsToFilter.Add(excluded);
                }
            }
            if (!ExceptionFilters.Instance.FlagFormatExceptions)
            {
                var excluded = Compilation.GetTypeByMetadataName("System.FormatException");
                if (excluded != null)
                {
                    exceptionsToFilter.Add(excluded);
                }
            }
            if (!ExceptionFilters.Instance.FlagOverflowExceptions)
            {
                var excluded = Compilation.GetTypeByMetadataName("System.OverflowException");
                if (excluded != null)
                {
                    exceptionsToFilter.Add(excluded);
                }
            }
            if (!ExceptionFilters.Instance.FlagAssertFailedExceptions)
            {
                var excluded = Compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.AssertFailedException");
                if (excluded != null)
                {
                    exceptionsToFilter.Add(excluded);
                }
            }
            if (!ExceptionFilters.Instance.FlagNotSupportedExceptions)
            {
                var excluded = Compilation.GetTypeByMetadataName("System.NotSupportedException");
                if (excluded != null)
                {
                    exceptionsToFilter.Add(excluded);
                }
            }
            if (!ExceptionFilters.Instance.FlagNotImplementedExceptions)
            {
                var excluded = Compilation.GetTypeByMetadataName("System.NotImplementedException");
                if (excluded != null)
                {
                    exceptionsToFilter.Add(excluded);
                }
            }

            // Exclude including subtypes
            return exceptions.Where(e => !exceptionsToFilter.Any(f => e.ExceptionType != null && Compilation.ClassifyConversion(e.ExceptionType, f).IsImplicit));
        }

        /// <summary>
        /// Extract list of exceptions declared as being thrown in a documentation comment.
        /// Exclude exceptions that are specifically marked with "Ignore"
        /// </summary>
        /// <param name="documentationCommentXml"></param>
        /// <param name="ignore">Ignore nodes marked with "Ignore"</param>
        /// <returns></returns>
        protected HashSet<ITypeSymbol> GetDeclaredExceptions(ISymbol symbol, bool ignore, AccessorKind accessorKind = AccessorKind.None)
        {
            return new DocumentationComment(Compilation).GetDeclaredExceptions(symbol, ignore, accessorKind);
        }

        protected ImmutableDictionary<string, string> GetDiagnosticProperties(SemanticModelAnalysisContext context, MemberDeclarationSyntax memberDeclarationSyntax, string concatenatedExceptions)
        {
            var properties = ImmutableDictionary.Create<string, string>();
            properties = properties.Add(ExceptionTypesProperty, concatenatedExceptions);
            properties = properties.Add(IsPrivateMethodProperty, ShouldEmitShortComment(context.SemanticModel, memberDeclarationSyntax).ToString());
            return properties;
        }

        private bool ShouldEmitShortComment(SemanticModel semanticModel, MemberDeclarationSyntax memberDeclarationSyntax)
        {
            if (memberDeclarationSyntax == null)
            {
                return false;
            }

            var declaredSymbol = semanticModel.GetDeclaredSymbol(memberDeclarationSyntax);
            if (declaredSymbol != null)
            {
                switch (declaredSymbol.DeclaredAccessibility)
                {
                    case Accessibility.Private:
                        return ShortCommentConfiguration.Instance.ForPrivate;
                    case Accessibility.Protected:
                    case Accessibility.ProtectedAndFriend:
                    case Accessibility.ProtectedOrInternal:
                        return ShortCommentConfiguration.Instance.ForProtected;
                    case Accessibility.Public:
                        return ShortCommentConfiguration.Instance.ForPublic;
                    case Accessibility.Internal:
                        return ShortCommentConfiguration.Instance.ForInternal;
                    default:
                        return false;
                }

            }
            return false;
        }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterCompilationStartAction(CompilationStartAction);
            context.RegisterSemanticModelAction(AnalyzeSemanticAction);
        }
    }
}
