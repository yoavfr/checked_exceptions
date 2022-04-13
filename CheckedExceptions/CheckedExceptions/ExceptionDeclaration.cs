using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace CheckedExceptions
{
    public class ExceptionDeclaration
    {
        public string Name { get; }
        public string ShortName { get; }
        public string Comment { get; }
        public bool Ignore { get; }
        public AccessorKind AccessorKind { get; }

        public ExceptionDeclaration(string name, string shortName, string comment, bool ignore, AccessorKind accessorKind)
        {
            Name = name;
            ShortName = shortName;
            Comment = comment;
            Ignore = ignore;
            AccessorKind = accessorKind;
        }
    }
}
