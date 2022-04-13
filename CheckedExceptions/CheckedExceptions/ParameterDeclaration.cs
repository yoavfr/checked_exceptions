using System;
using System.Collections.Generic;
using System.Text;

namespace CheckedExceptions
{
    public class ParameterDeclaration
    {
        public string Name { get; }
        public string Text { get; }

        public ParameterDeclaration(string name, string text)
        {
            Name = name;
            Text = text;
        }
    }
}
