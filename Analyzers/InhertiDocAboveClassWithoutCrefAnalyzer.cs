using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class InheritDocAboveClassWithoutCrefAnalyzer : DiagnosticAnalyzer
    {
        private const string Title = "Define cref for inheritdoc.";
        private const string MessageFormat = "Class is not allowed to have single inheritdoc without cref if it only implements one interface..";
        private const string Description = "Define cref for inheritdoc.";
        public const string RuleId = "XmlClassInheritdocEmpty100";

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
            context.RegisterSyntaxNodeAction(AnalyzeForEmptyInheritDoc, SyntaxKind.ClassDeclaration);
        }

        private void AnalyzeForEmptyInheritDoc(SyntaxNodeAnalysisContext analysisContext)
        {
            if (!(analysisContext.Node is ClassDeclarationSyntax classDeclarationSyntax))
            {
                return;
            }
            
            if (!classDeclarationSyntax.HasLeadingTrivia)
            {
                return;
            }

            INamedTypeSymbol classSymbol = analysisContext.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);

            if (classSymbol == null)
            {
                return;
            }
            
            int interfacesCount = classSymbol.Interfaces.Length;

            if (interfacesCount != 1 || classSymbol.BaseType!.Name != "Object")
            {
                return;
            }
            
            SyntaxTriviaList syntaxTriviaList = classDeclarationSyntax.GetLeadingTrivia();
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
            
            XmlEmptyElementSyntax inheritDoc = GetEmptyInheritDocElement(comment);

            if (inheritDoc == null)
            {
                return;
            }

            bool hasCrefAttribute = inheritDoc.Attributes.Any(attr => attr.Name.ToString() == "cref");
            if (hasCrefAttribute)
            {
                return;
            }
            
            Diagnostic diagnostic = Diagnostic.Create(Rule, inheritDoc.GetLocation());
            analysisContext.ReportDiagnostic(diagnostic);
        }
        
        private static XmlEmptyElementSyntax GetEmptyInheritDocElement(DocumentationCommentTriviaSyntax comment)
        {
            return comment!.Content.FirstOrDefault(element =>
            {
                if (element is not XmlEmptyElementSyntax emptyElement)
                {
                    return false;
                }

                string tagName = emptyElement.Name.LocalName.ToString();
                return tagName == "inheritdoc";
            }) as XmlEmptyElementSyntax;
        }
    }
}