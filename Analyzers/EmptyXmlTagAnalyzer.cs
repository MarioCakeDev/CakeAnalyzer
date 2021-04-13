using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzers
{
    /// <summary>
    /// Analyzer which checks that XML Comments have no empty tags defined.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class EmptyXmlTagAnalyzer : DiagnosticAnalyzer
    {
        private const string Title = "Fill empty XML Tag.";
        private const string MessageFormat = "XML Tag is not allowed to be empty.";
        private const string Description = "Fill empty XML Tag.";
        public const string RuleId = "XmlTagEmpty100";

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
            context.RegisterSyntaxNodeAction(AnalyzeCommentForEmptyTag,
                SyntaxKind.SingleLineDocumentationCommentTrivia,
                SyntaxKind.MultiLineDocumentationCommentTrivia
            );
        }

        private static void AnalyzeCommentForEmptyTag(SyntaxNodeAnalysisContext startCodeBlockContext)
        {
            DocumentationCommentTriviaSyntax syntaxNode = (DocumentationCommentTriviaSyntax)startCodeBlockContext.Node;
            
            IEnumerable<XmlNodeSyntax> xmlElements = GetEmptyXmlElements(syntaxNode);

            foreach (XmlNodeSyntax xmlElementSyntax in xmlElements)
            {
                Diagnostic diagnostic = Diagnostic.Create(Rule, xmlElementSyntax.GetLocation());
                startCodeBlockContext.ReportDiagnostic(diagnostic);
            }
        }

        private static IEnumerable<XmlNodeSyntax> GetEmptyXmlElements(DocumentationCommentTriviaSyntax comment)
        {
            IEnumerable<XmlNodeSyntax> xmlElements = comment!.Content.SelectMany(element =>
            {
                if (element is XmlEmptyElementSyntax emptyElement)
                {
                    string tagName = emptyElement.Name.LocalName.ToString();
                    return tagName == "inheritdoc" || tagName == "seealso"
                        ? new XmlNodeSyntax[0]
                        : new[] {emptyElement};
                }

                if (element is not XmlElementSyntax xmlElement)
                {
                    return new XmlNodeSyntax[0];
                }

                bool hasAny = xmlElement.Content.Any();
                if (!hasAny)
                {
                    return new[] {xmlElement};
                }

                bool areAllNodesEmpty = xmlElement.Content.All(node =>
                    node is XmlTextSyntax textSyntax &&
                    textSyntax.TextTokens.All(
                        textToken => String.IsNullOrWhiteSpace(textToken.Text)
                    )
                );

                return areAllNodesEmpty ? new[] {xmlElement} : new XmlNodeSyntax[0];
            });
            return xmlElements;
        }
    }
}