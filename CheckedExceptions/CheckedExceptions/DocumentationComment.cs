using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace CheckedExceptions
{
    class DocumentationComment
    {
        private Compilation Compilation { get; }

        public DocumentationComment(Compilation compilation)
        {
            Compilation = compilation;
        }

        /// <summary>
        /// Extract list of exceptions declared as being thrown in a documentation comment.
        /// Exclude exceptions that are specifically marked with "Ignore"
        /// </summary>
        /// <param name="documentationCommentXml"></param>
        /// <param name="ignore">Ignore nodes marked with "Ignore"</param>
        /// <returns></returns>
        public HashSet<ITypeSymbol> GetDeclaredExceptions(ISymbol symbol, bool ignore, AccessorKind accessorKind = AccessorKind.None)
        {
            var result = new HashSet<ITypeSymbol>();
            if (symbol == null)
            {
                return result;
            }
            var documentationCommentXml = symbol.GetDocumentationCommentXml();
            var accessorPrefix = accessorKind != AccessorKind.None ? accessorKind.ToString() : string.Empty;
            var ignorePrefix = "Ignore";
            var ignoreIndex = accessorPrefix.Length;
            if (string.IsNullOrEmpty(documentationCommentXml))
            {
                return result;
            }
            try
            {
                var doc = XDocument.Parse(documentationCommentXml);

                // recurse into base classes and interfaces for <inheritdoc/>
                var inheritDoc = doc.Descendants().Descendants("inheritdoc");
                if (inheritDoc.Count() > 0)
                {
                    // base class
                    var baseType = symbol.ContainingType.BaseType;
                    if (baseType != null)
                    {
                        var baseSymbol = baseType.GetMembers().OfType<ISymbol>().FirstOrDefault(p => p.Name == symbol.Name);
                        if (baseSymbol != null)
                        {
                            result.UnionWith(GetDeclaredExceptions(baseSymbol, ignore, accessorKind));
                        }
                    }

                    // interfaces
                    var methodDeclarationsInInterface = symbol.ContainingType.AllInterfaces.SelectMany(i => i.GetMembers().OfType<ISymbol>()).Where(
                    (interfaceMethod) =>
                    {
                        var implementation = symbol.ContainingType.FindImplementationForInterfaceMember(interfaceMethod);
                        return implementation != null && implementation.Equals(symbol);
                    });

                    foreach (var implementedMethod in methodDeclarationsInInterface)
                    {
                        result.UnionWith(GetDeclaredExceptions(implementedMethod, ignore, accessorKind));
                    }
                }

                // look for exception declarations
                var exceptionNodes = doc.Descendants().Descendants("exception");
                foreach (var node in exceptionNodes)
                {
                    if (ignore &&
                        node.Value.Length > ignoreIndex &&
                        node.Value.Substring(ignoreIndex).Trim(' ', '.').TrimStart().StartsWith(ignorePrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    // If this is a getter comment and we are looking at a setter or vice versa - ignore. 
                    if ((accessorKind == AccessorKind.Set || accessorKind == AccessorKind.Get) && !node.Value.TrimStart().StartsWith(accessorPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    var cref = node.Attribute("cref");
                    if (cref != null)
                    {
                        var cRefSymbol = Compilation.GetTypeByMetadataName(cref.Value.Substring(2));
                        if (cRefSymbol != null)
                        {
                            result.Add(cRefSymbol);
                        }
                    }
                }
            }
            catch (XmlException)
            {
            }
            return result;
        }

    }
}
