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
    public class NoXmlCommentAnalyzer : DiagnosticAnalyzer
    {
        private const string Title = "Missing XML Comment.";
        private const string MessageFormat = "Element needs to have an xml comment.";
        private const string Description = "Missing XML Comment.";
        public const string RuleId = "XmlMissing";

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

        private static readonly DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(
                RuleId,
                Title,
                MessageFormat,
                "XMLAnalyzer",
                DiagnosticSeverity.Error,
                true,
                Description);

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
                        ReportOnIdentifierToken(syntaxNode, startCodeBlockContext);
                        return;
                    }

                    SyntaxTriviaList syntaxTriviaList = syntaxNode.GetLeadingTrivia();
                    SyntaxTrivia xmlComment = syntaxTriviaList.FirstOrDefault(trivia =>
                        trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia));

                    if (xmlComment == default)
                    {
                        ReportOnIdentifierToken(syntaxNode, startCodeBlockContext);
                        return;
                    }

                    DocumentationCommentTriviaSyntax comment =
                        (DocumentationCommentTriviaSyntax) xmlComment.GetStructure();
                    bool allTextElements = comment!.Content.All(element => element is XmlTextSyntax);
                    if (allTextElements)
                    {
                        ReportOnIdentifierToken(syntaxNode, startCodeBlockContext);
                    }
                },
                CheckingNodes
            );
        }

        private static void ReportOnIdentifierToken(SyntaxNode syntaxNode,
            SyntaxNodeAnalysisContext startCodeBlockContext)
        {
            IEnumerable<SyntaxToken> childTokens = syntaxNode.ChildTokens();
            SyntaxToken identifierToken = childTokens
                .FirstOrDefault(token => token.IsKind(SyntaxKind.IdentifierToken));

            if (identifierToken == default)
            {
                identifierToken = syntaxNode switch
                {
                    BaseFieldDeclarationSyntax baseFieldDeclaration => baseFieldDeclaration.Declaration.Variables
                        .First()
                        .Identifier,
                    IndexerDeclarationSyntax indexerDeclarationSyntax => indexerDeclarationSyntax.ThisKeyword,
                    OperatorDeclarationSyntax operatorDeclarationSyntax => operatorDeclarationSyntax.OperatorKeyword,
                    _ => throw new NotSupportedException($"'{syntaxNode}' cannot be analysed.")
                };
            }

            Diagnostic diagnostic = Diagnostic.Create(Rule,
                identifierToken.GetLocation());
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