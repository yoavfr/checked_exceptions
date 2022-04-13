﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CheckedExceptions
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class EventHandlerExceptionAnalyzer : BaseUnhandledExceptionsAnalyzer
    {
        // Constants
        public const string EventHandlerExceptionsDiagnosticId = "EventHandlerExceptions";
        private static readonly string EventHandlerExceptionsTitle = "Exception(s) thrown from event handler";
        public static readonly string EventHandlerExceptionsMessageFormat = "Exception(s) thrown from event handler {0}: {1}. You probably don't want to throw exceptions from an event handler.";
        private static readonly string EventHandlerExceptionsDescription = "It is inadvisable to throw an exception in an event handler";
        private static readonly DiagnosticDescriptor EventHandlerExceptionsRule = new DiagnosticDescriptor(
            EventHandlerExceptionsDiagnosticId,
            EventHandlerExceptionsTitle,
            EventHandlerExceptionsMessageFormat,
            "Microsoft.Design",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: EventHandlerExceptionsDescription);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(EventHandlerExceptionsRule); } }

        /// <summary>
        /// Analyze exceptions thrown in event handlers - this is not a good practice as you are not guaranteed as to who will catch the exception if at all.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="semanticModel"></param>
        /// <param name="context"></param>
        /// <exception cref="System.InvalidOperationException">Ignore.</exception>
        protected override void Analyze(SyntaxNode root, SemanticModel semanticModel, SemanticModelAnalysisContext context)
        {
            // Get list of exceptions thrown by event handlers grouped by their location in the syntax tree
            var visitor = new EventHandlerExceptionVisitor(Compilation, semanticModel, root);
            visitor.Visit(root);
            var violations = GroupByLocation(visitor.Exceptions);

            // foreach location
            foreach (var violation in violations)
            {
                // create list of unhandled exceptions to pass into the Diagnostic
                var concatenatedExceptions = string.Join(",", violation.Value.Select(t => t.ExceptionType.ToString()));
                var prettyPrintConcatenatedExceptions = string.Join(", ", violation.Value.Select(t => t.ExceptionType.Name));

                // Report a diagnostic with the list of exceptions generated by that event handler
                var diagnostic = Diagnostic.Create(
                    descriptor: EventHandlerExceptionsRule,
                    location: violation.Key,
                    properties: null,
                    messageArgs: new string[] { violation.Value.First().EventHandlerName, prettyPrintConcatenatedExceptions });
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
