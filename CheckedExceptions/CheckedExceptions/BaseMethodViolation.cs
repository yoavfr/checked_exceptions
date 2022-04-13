using Microsoft.CodeAnalysis;

namespace CheckedExceptions
{
    public class BaseMethodViolation : Violation
    {
        public INamedTypeSymbol Interface { get; }
        public ISymbol Method { get; }

        public BaseMethodViolation(ITypeSymbol exceptionType, INamedTypeSymbol @interface, ISymbol method, Location location) : base (exceptionType, location)
        {
            Interface = @interface;
            Method = method;
        }
    }
}
