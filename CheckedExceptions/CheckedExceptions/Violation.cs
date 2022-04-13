using Microsoft.CodeAnalysis;

namespace CheckedExceptions
{
    public class Violation
    {
        public ITypeSymbol ExceptionType { get; }
        public Location Location { get; }

        public Violation (ITypeSymbol exceptionType, Location location)
        {
            ExceptionType = exceptionType;
            Location = location;
        }

        public override bool Equals(object obj)
        {
            var other = obj as Violation;
            if (other == null)
            {
                return false;
            }
            return ExceptionType.Equals(other.ExceptionType) && Location.Equals(other.Location);
        }

        public override int GetHashCode()
        {
            return ExceptionType.GetHashCode() + Location.GetHashCode();
        }
    }
}
