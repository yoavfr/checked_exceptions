using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;

namespace CheckedExceptions
{
    public class XmlCommentBuilder
    {
        private readonly SyntaxNode m_SyntaxNode;
        private readonly string m_Title;
        private readonly string m_ReturnType;
        private readonly List<ParameterDeclaration> m_Parameters;
        private readonly List<ExceptionDeclaration> m_Exceptions;
        private readonly bool m_ShortComment;
        private string m_OldTrivia;
        private static Regex s_ExceptionRegex = new Regex(string.Format(@"///\s*<exception cref=""(?<exceptionName>.*)"">(?<accessor>{0}.|{1}.)*(?<insertionPoint>$*)\s*(?<ignore>Ignore.)*\s*(?<text>.*)</exception>", AccessorKind.Get, AccessorKind.Set), RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public XmlCommentBuilder(SyntaxNode syntaxNode, IEnumerable<ExceptionDeclaration> exceptions, bool shortComment, string title = null, string returnType = null, IEnumerable<ParameterDeclaration> parameters = null)
        {
            m_SyntaxNode = syntaxNode;
            m_Title = title;
            m_ReturnType = returnType;
            if (parameters != null)
            {
                m_Parameters = new List<ParameterDeclaration>(parameters);
            }
            else
            {
                m_Parameters = new List<ParameterDeclaration>();
            }
            m_Exceptions = new List<ExceptionDeclaration>(exceptions);
            m_ShortComment = shortComment;

            var xmlTrivia = syntaxNode.GetLeadingTrivia()
               .Select(i => i.GetStructure())
               .OfType<DocumentationCommentTriviaSyntax>()
               .FirstOrDefault();

            if (xmlTrivia != null)
            {
                Parse(syntaxNode);
            }
        }

        public SyntaxTrivia ToSyntaxTrivia()
        {
            if (m_OldTrivia != null)
            {
                return AppendToExisting();
            }
            else if (m_ReturnType == null)
            {
                return ConstructorToSyntaxTrivia();
            }
            else
            {
                return MethodToSyntaxTrivia();
            }
        }

        private void Parse (SyntaxNode syntaxNode)
        {
            // Current XML comment
            m_OldTrivia = syntaxNode.GetLeadingTrivia().ToFullString().TrimEnd();

            // Find existing exception declarations
            var matches = s_ExceptionRegex.Matches(m_OldTrivia);

            // For each existing declaration (go in reverse so indexes are correct)
            for (int i = matches.Count - 1; i >=0; i--)
            {
                var exceptionName = matches[i].Groups["exceptionName"].ToString();
                var text = matches[i].Groups["text"].ToString();
                var textIndex = matches[i].Groups["text"].Index;
                var ignore = matches[i].Groups["ignore"];
                var accessor = matches[i].Groups["accessor"].ToString();
                var insertionPoint = matches[i].Groups["insertionPoint"].Index;
                var kind = AccessorKind.None;

                // If we are updating an accessor
                if (syntaxNode is PropertyDeclarationSyntax && accessor != string.Empty)
                {
                    // Check if this exception is for a getter
                    if (accessor.StartsWith(AccessorKind.Get.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        kind = AccessorKind.Get;
                    }
                    // or a setter
                    else if (accessor.StartsWith(AccessorKind.Set.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        kind = AccessorKind.Set;
                    }
                }

                // See if it matches one of the exceptions we want to add or update
                var matchingInExisting = m_Exceptions.FirstOrDefault(e => (e.Name == exceptionName || e.ShortName == exceptionName) && e.AccessorKind == kind);
                if (matchingInExisting != null)
                {
                    // If we want to ignore this exception and it is not already marked as such
                    if (matchingInExisting.Ignore && !ignore.Success)
                    {
                        // <exception>Getter.</exception> => <exception>Getter. Ignore.</exception>
                        var paddingBefore = accessor == string.Empty ? string.Empty : " ";

                        // <exception>Some Text</exception> => <exception>Ignore. Some Text</exception>
                        var paddingAfter = text != string.Empty && accessor == string.Empty ? " " : string.Empty;
                        
                        // insert Ignore in the right location (after the getter/setter if exists)
                        m_OldTrivia = m_OldTrivia.Insert(insertionPoint, paddingBefore + "Ignore." + paddingAfter);
                    }

                    // remove this exception from the list - it already exists
                    m_Exceptions.Remove(matchingInExisting);
                }
            }
        }

        private SyntaxTrivia ConstructorToSyntaxTrivia()
        {
            // indentation
            var indentation = GetIndentation(m_SyntaxNode);

            // exceptions
            var exceptionsComment = GetExceptionsAsCommentString(indentation);

            // Whitespace before the method declaration
            var leadingTrivia = m_SyntaxNode.GetLeadingTrivia().ToFullString();

            // Short comment - emmit only the exceptions
            if (m_ShortComment)
            {
                return GetShortComment(exceptionsComment, leadingTrivia);
            }

            // parameters
            var parameterCommentString = @"
{0}/// <param name=""{1}"">{2}</param>";
            var parametersComment = new StringBuilder();
            foreach (var parameter in m_Parameters)
            {
                parametersComment.Append(string.Format(parameterCommentString, indentation, parameter.Name, parameter.Text));
            }

            // Putting it all together - the full xml comment
            var commentString = @"{0}/// <summary>
{1}/// {2}
{1}/// </summary>{3}{4}
{1}";
            var comment = SyntaxFactory.Comment(string.Format(commentString, leadingTrivia, indentation, m_Title, parametersComment.ToString(), exceptionsComment));
            return comment;
        }

        private SyntaxTrivia MethodToSyntaxTrivia()
        {
            // indentation
            var indentation = GetIndentation(m_SyntaxNode);

            // exceptions
            var exceptionsComment = GetExceptionsAsCommentString(indentation);

            // Whitespace before the method declaration
            var leadingTrivia = m_SyntaxNode.GetLeadingTrivia().ToFullString();

            // Short comment - emmit only the exceptions
            if (m_ShortComment)
            {
                return GetShortComment(exceptionsComment, leadingTrivia);
            }

            // parameters
            var parameterCommentString = @"
{0}/// <param name=""{1}"">{2}</param>";
            var parametersComment = new StringBuilder();
            foreach (var parameter in m_Parameters)
            {
                parametersComment.Append(string.Format(parameterCommentString, indentation, parameter.Name, parameter.Text));
            }

            // return type
            var returnCommentString = @"
{0}/// <returns></returns>";
            var returnComment = m_ReturnType != "void" ? string.Format(returnCommentString, indentation) : string.Empty;

            // Putting it all together - the full xml comment
            var commentFormat = @"{0}/// <summary>
{1}/// {2}
{1}/// </summary>{3}{4}{5}
{1}";
            var comment = SyntaxFactory.Comment(string.Format(commentFormat, leadingTrivia, indentation, m_Title, parametersComment.ToString(), returnComment, exceptionsComment));
            return comment;
        }

        private SyntaxTrivia AppendToExisting()
        {
            var indentation = GetIndentation(m_SyntaxNode);
            var commentFormat = @"{0}{1}
{2}";
            var comment = SyntaxFactory.Comment(string.Format(commentFormat, m_OldTrivia, GetExceptionsAsCommentString(indentation), indentation));
            return comment;
        }

        private string GetExceptionsAsCommentString(string tabbing)
        {
            // string of exceptions to add
            var exceptionCommentFormat = @"
{0}/// <exception cref=""{1}"">{2}{3}{4}</exception>";
            var exceptionsCommentBuilder = new StringBuilder();
            foreach (var exception in m_Exceptions)
            {
                var accessor = exception.AccessorKind == AccessorKind.None ? string.Empty : exception.AccessorKind.ToString() + ".";
                var ignore = exception.Ignore ? "Ignore." : string.Empty;
                var whitespace = ignore != string.Empty && accessor != string.Empty ? " " : string.Empty;
                exceptionsCommentBuilder.Append(string.Format(exceptionCommentFormat, tabbing, exception.ShortName, accessor, whitespace, ignore));
            }
            return exceptionsCommentBuilder.ToString();
        }

        private static string GetIndentation(SyntaxNode syntaxNode)
        {
            // get the indentation of the method we are decorating
            var trivia = syntaxNode.GetLeadingTrivia().Where(t => t.Kind() == SyntaxKind.WhitespaceTrivia).LastOrDefault();

            // zero indentation
            if (trivia == default(SyntaxTrivia))
            {
                return string.Empty;
            }
            return new SyntaxTriviaList().Add(trivia).ToFullString();
        }

        private static SyntaxTrivia GetShortComment(string exceptionsCommentString, string leadingTrivia)
        {
            exceptionsCommentString = exceptionsCommentString.Substring(2);
            var shortCommentString = @"{0}
{1}";
            return SyntaxFactory.Comment(string.Format(shortCommentString, exceptionsCommentString, leadingTrivia));
        }

    }
}
