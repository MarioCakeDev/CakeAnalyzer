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
    public class SummaryContentEqualsElementNameAnalyzer : DiagnosticAnalyzer
    {
        private const string Title = "Summary is too simple.";

        private const string MessageFormat =
            "Summary of an element should not only contain the name of the element.";

        private const string Description = "Summary is too simple.";
        public const string RuleId = "XmlSummaryTagTooSimple100";

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

        
        private static readonly SyntaxKind[] CheckingNodes =
        {
            SyntaxKind.ClassDeclaration,
            SyntaxKind.EnumDeclaration,
            SyntaxKind.InterfaceDeclaration,
            SyntaxKind.StructDeclaration,
            
            SyntaxKind.EventFieldDeclaration,
            SyntaxKind.FieldDeclaration,
            
            SyntaxKind.PropertyDeclaration,
            
            SyntaxKind.MethodDeclaration,
            
            SyntaxKind.ConstructorDeclaration,
            
            SyntaxKind.DestructorDeclaration,
            
            SyntaxKind.EnumMemberDeclaration,
            
            SyntaxKind.EventDeclaration,
            
            SyntaxKind.DelegateDeclaration
        };

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeSimpleXmlComment,
                CheckingNodes
            );
        }

        private void AnalyzeSimpleXmlComment(SyntaxNodeAnalysisContext startCodeBlockContext)
        {
            SyntaxNode syntaxNode = startCodeBlockContext.Node;
            bool shouldCheckXmlComment = ShouldCheckXmlComment(syntaxNode);
            if (!shouldCheckXmlComment)
            {
                return;
            }

            if (!syntaxNode.HasLeadingTrivia)
            {
                return;
            }

            SyntaxTriviaList syntaxTriviaList = syntaxNode.GetLeadingTrivia();
            SyntaxTrivia xmlComment = syntaxTriviaList.FirstOrDefault(trivia =>
                trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia));

            if (xmlComment == default)
            {
                return;
            }

            DocumentationCommentTriviaSyntax comment =
                (DocumentationCommentTriviaSyntax) xmlComment.GetStructure();
            bool allTextElements = comment!.Content.All(element => element is XmlTextSyntax);
            if (allTextElements)
            {
                return;
            }

            XmlNodeSyntax summaryNode = comment!.Content.FirstOrDefault(node => string.Equals((node as XmlElementSyntax)?.StartTag.Name.ToString(), "summary", StringComparison.InvariantCultureIgnoreCase));

            if (summaryNode is null)
            {
                return;
            }

            IEnumerable<SyntaxNode> syntaxNodesWithoutStartAndEnd = summaryNode.ChildNodes().Skip(1).Reverse().Skip(1).Reverse();

            Regex whitespace = new Regex("\\s", RegexOptions.Multiline);
            IEnumerable<string> onlyTextTokens = syntaxNodesWithoutStartAndEnd.Select(x => x as XmlTextSyntax)
                .Select(x => string.Join("", x.TextTokens.Select(c => whitespace.Replace(c.Text, ""))).Trim());
            string content = string.Join("", onlyTextTokens).Trim();

            string elementName = GetElementName(syntaxNode);

            if (content.Equals(elementName, StringComparison.InvariantCultureIgnoreCase))
            {
                ReportOnIdentifierToken(summaryNode, startCodeBlockContext);
            }
        }

        private static string GetElementName(SyntaxNode syntaxNode)
        {
            return syntaxNode switch
            {
                BaseFieldDeclarationSyntax fieldDeclarationSyntax => string.Join(",", fieldDeclarationSyntax.Declaration.Variables.Select(variable => variable.Identifier.Text)),
                BaseTypeDeclarationSyntax typeDeclarationSyntax => typeDeclarationSyntax.Identifier.Text,
                PropertyDeclarationSyntax propertyDeclarationSyntax => propertyDeclarationSyntax.Identifier.Text,
                MethodDeclarationSyntax methodDeclarationSyntax => methodDeclarationSyntax.Identifier.Text,
                ConstructorDeclarationSyntax constructorDeclarationSyntax => constructorDeclarationSyntax.Identifier.Text,
                DestructorDeclarationSyntax destructorDeclarationSyntax => destructorDeclarationSyntax.Identifier.Text,
                EnumMemberDeclarationSyntax enumMemberDeclarationSyntax => enumMemberDeclarationSyntax.Identifier.Text,
                EventDeclarationSyntax eventDeclarationSyntax => eventDeclarationSyntax.Identifier.Text,
                DelegateDeclarationSyntax delegateDeclarationSyntax => delegateDeclarationSyntax.Identifier.Text,
                _ => throw new InvalidOperationException($"Invalid syntax node {syntaxNode.GetType()}")
            };
        }

        private static void ReportOnIdentifierToken(SyntaxNode syntaxNode,
            SyntaxNodeAnalysisContext startCodeBlockContext)
        {
            Diagnostic diagnostic = Diagnostic.Create(Rule, syntaxNode.GetLocation());
            startCodeBlockContext.ReportDiagnostic(diagnostic);
        }

        private bool ShouldCheckXmlComment(SyntaxNode syntaxNode)
        {
            return !ShouldBeIgnored(syntaxNode) &&
                   (
                       IsPublic(syntaxNode) ||
                       IsProtected(syntaxNode) ||
                       IsClass(syntaxNode) ||
                       IsInterface(syntaxNode) ||
                       IsEnum(syntaxNode) ||
                       IsStruct(syntaxNode) ||
                       IsInternal(syntaxNode) ||
                       IsDestructor(syntaxNode) ||
                       IsEnumMember(syntaxNode)
                   );
        }

        /// <summary>
        /// Ignore the check if the method declaration is a Fact test or a Theory test from xUnit.
        /// </summary>
        /// <param name="syntaxNode">The syntax node which is checked whether is has the attributes above them.</param>
        /// <returns>
        /// Returns true if the method declaration contains the Fact attribute or the Theory attribute above it.
        /// </returns>
        private bool ShouldBeIgnored(SyntaxNode syntaxNode)
        {
            if (syntaxNode is not MethodDeclarationSyntax methodDeclarationSyntax)
            {
                return false;
            }

            return methodDeclarationSyntax.AttributeLists.Any(attributeList =>
            {
                return attributeList.Attributes.Any(attribute =>
                {
                    string attributeName = attribute.Name.ToString();
                    return attributeName == "Fact" || attributeName == "Theory";
                });
            });
        }

        private bool IsEnumMember(SyntaxNode syntaxNode)
        {
            return syntaxNode.IsKind(SyntaxKind.EnumMemberDeclaration);
        }

        private bool IsStruct(SyntaxNode syntaxNode)
        {
            return syntaxNode.Kind() == SyntaxKind.StructDeclaration;
        }

        private bool IsEnum(SyntaxNode syntaxNode)
        {
            return syntaxNode.Kind() == SyntaxKind.EnumDeclaration;
        }

        private bool IsInterface(SyntaxNode syntaxNode)
        {
            return syntaxNode.Kind() == SyntaxKind.InterfaceDeclaration;
        }

        private bool IsClass(SyntaxNode syntaxNode)
        {
            return syntaxNode.Kind() == SyntaxKind.ClassDeclaration;
        }

        private bool IsProtected(SyntaxNode syntaxNode)
        {
            return syntaxNode.ChildTokens().Any(token => token.IsKind(SyntaxKind.ProtectedKeyword));
        }

        private bool IsPublic(SyntaxNode syntaxNode)
        {
            return syntaxNode.ChildTokens().Any(token => token.IsKind(SyntaxKind.PublicKeyword));
        }

        private bool IsInternal(SyntaxNode syntaxNode)
        {
            return syntaxNode.ChildTokens().Any(token => token.IsKind(SyntaxKind.InternalKeyword));
        }

        private bool IsDestructor(SyntaxNode syntaxNode)
        {
            return syntaxNode.IsKind(SyntaxKind.DestructorDeclaration);
        }
    }
}