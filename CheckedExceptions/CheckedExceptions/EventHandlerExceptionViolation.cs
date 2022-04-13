using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace CheckedExceptions
{
    class EventHandlerExceptionViolation : Violation
    {
        public string EventHandlerName { get; }
        public EventHandlerExceptionViolation(ITypeSymbol exceptionType, SyntaxNode node, string eventHandlerName) : base(exceptionType, node.GetLocation())
        {
            EventHandlerName = eventHandlerName;
        }
    }
}
