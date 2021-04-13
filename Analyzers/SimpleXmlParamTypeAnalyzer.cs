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
    public class SimpleXmlParamTypeAnalyzer : DiagnosticAnalyzer
    {
        private const string Title = "Parameter is too simple.";

        private const string MessageFormat =
            "Param tag in XML comment is not allowed to be a form of the type name itself.";

        private const string Description = "Parameter is too simple.";
        public const string RuleId = "XmlParameterTagTooSimpleByType100";

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

                    IReadOnlyList<ParameterSyntax> parameters = GetParameters(syntaxNode);

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
                    List<XmlElementSyntax> paramTagNameValues = paramTags
                        .Select(tag => (XmlElementSyntax)tag)
                        .ToList();

                    Regex invalidIdentifierChars = new("[^a-zA-Z0-9_]", RegexOptions.Multiline);
                    Regex xmlCommentTokens = new("(///|\\*)", RegexOptions.Multiline);
                    Regex ofFromThe = new("\\b(of|from|the|an?)\\b", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                    
                    IEnumerable<(ParameterSyntax parameter, XmlElementSyntax tag)> parameterTagMap = parameters.Join(
                            paramTagNameValues,
                            parameter => parameter.Identifier.Text.ToLower(),
                            tag => (tag.StartTag.Attributes.FirstOrDefault(attribute =>
                                attribute.Name.ToString() == "name") as XmlNameAttributeSyntax)?.Identifier.ToString(),
                            (parameter, tag) => (parameter, tag)
                        )
                        .Where(tuple =>
                        {
                            (ParameterSyntax parameter, XmlElementSyntax tag) = tuple;
                            string parameterType = parameter.Type!.ToString();
                            
                            string paramTagComment = xmlCommentTokens.Replace(tag.Content.ToString(), "");
                            paramTagComment = ofFromThe.Replace(paramTagComment, "");
                            string contentWithoutWhitespace = invalidIdentifierChars.Replace(paramTagComment, "");

                            if (string.Equals(contentWithoutWhitespace, parameterType, StringComparison.InvariantCultureIgnoreCase))
                            {
                                return true;
                            }

                            string[] commentParts = paramTagComment.Trim().Split(' ', '\t', '\n');

                            if (commentParts.Length == 1)
                            {
                                return false;
                            }

                            string firstPart = commentParts[0];
                            for (int i = 1; i < commentParts.Length; i++)
                            {
                                commentParts[i - 1] = commentParts[i];
                            }

                            commentParts[commentParts.Length - 1] = firstPart;
                            contentWithoutWhitespace = invalidIdentifierChars.Replace(string.Join("", commentParts), "");
                
                            return string.Equals(contentWithoutWhitespace, parameterType, StringComparison.InvariantCultureIgnoreCase);
                        });

                    foreach ((ParameterSyntax _, XmlElementSyntax tag) in parameterTagMap)
                    {
                        Diagnostic diagnostic = Diagnostic.Create(Rule, tag.GetLocation());
                        startCodeBlockContext.ReportDiagnostic(diagnostic);
                    }
                },
                CheckingNodes
            );
        }

        private IReadOnlyList<ParameterSyntax> GetParameters(SyntaxNode syntaxNode)
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