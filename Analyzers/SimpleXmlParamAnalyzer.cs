using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzers
{
    /// <summary>
    /// Checks whether the param tag contains a parameter which is too simple by comparing that the value of the xml
    /// tag exactly the parameter name.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SimpleXmlParamAnalyzer : DiagnosticAnalyzer
    {
        private const string Title = "Parameter is too simple.";

        private const string MessageFormat =
            "Param tag in XML comment is not allowed to be a form of the parameter name itself.";

        private const string Description = "Parameter is too simple.";
        public const string RuleId = "XmlParameterTagTooSimple100";

        private static readonly DiagnosticDescriptor Rule = new(
            RuleId,
            Title,
            MessageFormat,
            "XmlDocumentation",
            DiagnosticSeverity.Warning,
            true,
            Description
        );

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);


        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeSimpleXmlComment,
                SyntaxKind.SingleLineDocumentationCommentTrivia,
                SyntaxKind.MultiLineDocumentationCommentTrivia
            );
        }

        private static void AnalyzeSimpleXmlComment(SyntaxNodeAnalysisContext startCodeBlockContext)
        {
            DocumentationCommentTriviaSyntax syntaxNode = (DocumentationCommentTriviaSyntax) startCodeBlockContext.Node;

            IEnumerable<XmlElementSyntax> paramTags = GetParamXmlTags(syntaxNode);

            Regex whitespace = new("\\s", RegexOptions.Multiline);
            Regex xmlCommentTokens = new("(///|\\*)", RegexOptions.Multiline);
            foreach (XmlElementSyntax paramTag in paramTags)
            {
                string parameterName = (paramTag.StartTag.Attributes.FirstOrDefault(
                    attribute => attribute is XmlNameAttributeSyntax
                ) as XmlNameAttributeSyntax)?.Identifier.ToString();
                if (string.IsNullOrEmpty(parameterName))
                {
                    continue;
                }

                string contentWithoutWhitespace = whitespace.Replace(paramTag.Content.ToString(), "");
                contentWithoutWhitespace = xmlCommentTokens.Replace(contentWithoutWhitespace, "");

                if (!string.Equals(contentWithoutWhitespace, parameterName, StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }

                Diagnostic diagnostic = Diagnostic.Create(Rule, paramTag.GetLocation());
                startCodeBlockContext.ReportDiagnostic(diagnostic);
            }
        }
        
        private static IEnumerable<XmlElementSyntax> GetParamXmlTags(DocumentationCommentTriviaSyntax xmlDocumentation)
        {
            IEnumerable<XmlElementSyntax> xmlElements = xmlDocumentation!.Content.SelectMany(element =>
            {
                bool isXmlTag = element is XmlElementSyntax;
                if (!isXmlTag)
                {
                    return new XmlElementSyntax[0];
                }

                XmlElementSyntax xmlElement = (XmlElementSyntax) element;

                bool isParamTag = xmlElement.StartTag.Name.ToString() == "param";
                return isParamTag ? new[] {xmlElement} : new XmlElementSyntax[0];
            });
            return xmlElements;
        }
    }
}