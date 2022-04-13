using Microsoft.CodeAnalysis;

namespace CheckedExceptions
{
    public class UnhandledExceptionViolation : Violation
    {
        
        public SyntaxNode Node { get; }

        public UnhandledExceptionViolation (ITypeSymbol exceptionType, SyntaxNode node) : base(exceptionType, node.GetLocation())
        {
            Node = node;
        }
    }
}
