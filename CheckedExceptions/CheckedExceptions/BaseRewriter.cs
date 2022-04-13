using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace CheckedExceptions
{
    abstract public class BaseRewriter : CSharpSyntaxRewriter
    {
        private readonly Document m_Document;

        public BaseRewriter(Document document)
        {
            m_Document = document;
        }

        protected string ShortExceptionName(string fullExceptionName)
        {
            var lastPeriodIndex = fullExceptionName.LastIndexOf('.');
            if (lastPeriodIndex == -1)
            {
                return fullExceptionName;
            }
            var shortExceptionName = fullExceptionName.Substring(lastPeriodIndex + 1);
            var exceptionNamespace = fullExceptionName.Substring(0, lastPeriodIndex);

            var namespaces = m_Document.GetSyntaxRootAsync().Result.DescendantNodes().OfType<UsingDirectiveSyntax>().Select(d => d.Name.ToFullString());
            
            if (namespaces.Contains(exceptionNamespace))
            {
                return shortExceptionName;
            }

            var semanticModel = m_Document.GetSemanticModelAsync().Result;
            if (semanticModel != null && semanticModel.LookupSymbols(0, name: shortExceptionName).Length > 0)
            {
                return shortExceptionName;
            }

            return fullExceptionName;
        }
    }
}
