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
    /// abc
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class EmptyXmlTagAnalyzer : DiagnosticAnalyzer
    {
        private const string Title = "Fill empty XML Tag.";
        private const string MessageFormat = "XML Tag is not allowed to be empty.";
        private const string Description = "Fill empty XML Tag.";
        public const string RuleId = "XmlTagEmpty";

        private static readonly DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(
                RuleId,
                Title,
                MessageFormat,
                "XMLAnalyzer",
                DiagnosticSeverity.Warning,
                true,
                Description);

        private static readonly SyntaxKind[] CheckingNodes =
        {
            SyntaxKind.ClassDeclaration,
            SyntaxKind.MethodDeclaration,
            SyntaxKind.PropertyDeclaration,
            SyntaxKind.ConstructorDeclaration,
            SyntaxKind.EnumDeclaration,
            SyntaxKind.EnumMemberDeclaration,
            SyntaxKind.IndexerDeclaration,
            SyntaxKind.OperatorDeclaration,
            SyntaxKind.StructDeclaration,
            SyntaxKind.DestructorDeclaration,
            SyntaxKind.EventFieldDeclaration,
            SyntaxKind.EventDeclaration,
            SyntaxKind.DelegateDeclaration,
            SyntaxKind.InterfaceDeclaration
        };

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

                    // Ignore if no xml comment exists.
                    if (!syntaxNode.HasLeadingTrivia)
                    {
                        return;
                    }

                    SyntaxTriviaList syntaxTriviaList = syntaxNode.GetLeadingTrivia();
                    SyntaxTrivia xmlComment = syntaxTriviaList.FirstOrDefault(trivia =>
                        trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia));
                    // Ignore if no xml comment exists.
                    if (xmlComment == default)
                    {
                        return;
                    }

                    DocumentationCommentTriviaSyntax comment =
                        (DocumentationCommentTriviaSyntax) xmlComment.GetStructure();

                    IEnumerable<XmlNodeSyntax> xmlElements = comment!.Content.SelectMany(element =>
                    {
                        if (element is XmlEmptyElementSyntax emptyElement)
                        {
                            return emptyElement.Name.LocalName.ToString() == "inheritdoc"
                                ? new XmlNodeSyntax[0]
                                : new[] {emptyElement};
                        }

                        if (!(element is XmlElementSyntax xmlElement))
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

                    foreach (XmlNodeSyntax xmlElementSyntax in xmlElements)
                    {
                        Diagnostic diagnostic = Diagnostic.Create(Rule, xmlElementSyntax.GetLocation());
                        startCodeBlockContext.ReportDiagnostic(diagnostic);
                    }
                },
                CheckingNodes
            );
        }
        
        private bool ShouldCheckXmlComment(SyntaxNode syntaxNode)
        {
            return IsPublic(syntaxNode) ||
                   IsProtected(syntaxNode) ||
                   IsClass(syntaxNode) ||
                   IsInterface(syntaxNode) ||
                   IsEnum(syntaxNode) ||
                   IsStruct(syntaxNode) ||
                   IsInternal(syntaxNode) ||
                   IsEnumMember(syntaxNode) ||
                   IsDestructor(syntaxNode);
        }

        private bool IsDestructor(SyntaxNode syntaxNode)
        {
            return syntaxNode.IsKind(SyntaxKind.DestructorDeclaration);
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
    }
}