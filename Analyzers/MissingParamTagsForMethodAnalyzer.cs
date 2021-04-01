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
    /// Analyzer which checks that all declaration types defined in <see cref="CheckingNodes"/> have xml comments
    /// defined.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MissingParamTagsForMethodAnalyzer : DiagnosticAnalyzer
    {
        private const string Title = "Missing XML param xml comment.";
        private const string MessageFormat = "Method needs to have an xml comment for the paramether of a method.";
        private const string Description = "Missing XML param xml comment.";
        public const string RuleId = "XmlParamMissing100";

        private static readonly SyntaxKind[] CheckingNodes =
        {
            SyntaxKind.MethodDeclaration,
            SyntaxKind.ConstructorDeclaration,
            SyntaxKind.IndexerDeclaration,
            SyntaxKind.OperatorDeclaration
        };

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
            context.RegisterSyntaxNodeAction(startCodeBlockContext =>
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

                    IReadOnlyList<ParameterSyntax> parameters = GetParameterNames(syntaxNode);

                    if (parameters.Count == 0)
                    {
                        return;
                    }
                    
                    bool hasInheritDoc = comment!.Content.Any(node => string.Equals((node as XmlEmptyElementSyntax)?.Name.ToString(), "inheritdoc", StringComparison.InvariantCultureIgnoreCase));
                    if (hasInheritDoc)
                    {
                        return;
                    }
                    
                    IEnumerable<XmlNodeSyntax> paramTags = comment!.Content.Where(node => string.Equals((node as XmlElementSyntax)?.StartTag.Name.ToString(), "param", StringComparison.InvariantCultureIgnoreCase));
                    List<string> paramTagNameValues = paramTags.Select(tag =>
                        (((XmlElementSyntax) tag).StartTag.Attributes.FirstOrDefault(attribute =>
                            attribute.Name.ToString() == "name") as XmlNameAttributeSyntax)?.Identifier.ToString())
                        .Where(tag => !string.IsNullOrWhiteSpace(tag))
                        .ToList();

                    IEnumerable<ParameterSyntax> missingParamTexts = parameters.Where(parameter => !paramTagNameValues.Any(paramValue => string.Equals(paramValue, parameter.Identifier.Text, StringComparison.InvariantCultureIgnoreCase)));
                    foreach (ParameterSyntax missingParamText in missingParamTexts)
                    {
                        ReportOnIdentifierToken(missingParamText, startCodeBlockContext);
                    }
                },
                CheckingNodes
            );
        }

        private IReadOnlyList<ParameterSyntax> GetParameterNames(SyntaxNode syntaxNode)
        {
            return syntaxNode switch
            {
                MethodDeclarationSyntax methodDeclarationSyntax => methodDeclarationSyntax.ParameterList.Parameters,
                ConstructorDeclarationSyntax constructorDeclarationSyntax => constructorDeclarationSyntax.ParameterList.Parameters,
                OperatorDeclarationSyntax operatorDeclarationSyntax => operatorDeclarationSyntax.ParameterList.Parameters,
                IndexerDeclarationSyntax indexerDeclarationSyntax => indexerDeclarationSyntax.ParameterList.Parameters,
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
                       IsEnumMember(syntaxNode) ||
                       IsInInterface(syntaxNode)
                   );
        }

        private bool IsInInterface(SyntaxNode syntaxNode)
        {
            return syntaxNode.Parent is InterfaceDeclarationSyntax;
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