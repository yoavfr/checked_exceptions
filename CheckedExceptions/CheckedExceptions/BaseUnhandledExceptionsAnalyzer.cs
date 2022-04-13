using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CheckedExceptions
{
    public abstract class BaseUnhandledExceptionsAnalyzer : BaseAnalyzer
    {
        /// <summary>
        /// Get exceptions that are thrown by a method either in the method itself, or by calling some other method that is documented as throwing an exception
        /// </summary>
        /// <param name="semanticModel"></param>
        /// <param name="bodySyntax"></param>
        /// <returns></returns>
        protected HashSet<UnhandledExceptionViolation> GetThrownExceptions(SemanticModel semanticModel, SyntaxNode bodySyntax)
        {
            var thrownExceptions = new HashSet<UnhandledExceptionViolation>();

            if (bodySyntax == null)
            {
                return thrownExceptions;
            }

            var visitor = new UnhandledExceptionVisitor(Compilation, semanticModel, bodySyntax);
            visitor.Visit(bodySyntax);

            return visitor.Exceptions;
        }
    }
}
