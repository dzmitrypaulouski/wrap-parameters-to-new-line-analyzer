using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WrapParameters
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(WrapParametersCodeFixProvider)), Shared]
    public class WrapParametersCodeFixProvider : CodeFixProvider
    {
        // TODO: Get indentation from config (project/solution/editorconfig)
        private readonly string _tabSpace = "    ";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(WrapParametersAnalyzer.DiagnosticId);
            }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // TODO: Error handling
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var methodDeclaration = root
                .FindToken(diagnosticSpan.Start)
                .Parent
                .AncestorsAndSelf()
                .OfType<MethodDeclarationSyntax>()
                .First();

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.CodeFixTitle,
                    createChangedDocument: cancellationToken => 
                        WrapParametersAsync(context.Document, root, methodDeclaration, cancellationToken),
                    equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                diagnostic);
        }

        private async Task<Document> WrapParametersAsync(
            Document document,
            SyntaxNode root,
            MethodDeclarationSyntax methodDeclaration,
            CancellationToken cancellationToken)
        {
            var leadingWhiteSpace = methodDeclaration
                .GetLeadingTrivia()
                .Where(trivia => trivia.IsKind(SyntaxKind.WhitespaceTrivia))
                .ToSyntaxTriviaList()
                .Add(SyntaxFactory.ElasticWhitespace(_tabSpace));

            var newDeclaration = methodDeclaration
                .ReplaceNodes(
                    methodDeclaration.ParameterList.Parameters,
                    (_, node) => WrapParmeterToNewLine(node, leadingWhiteSpace));

            var newRoot = root.ReplaceNode(methodDeclaration, newDeclaration);

            return document.WithSyntaxRoot(newRoot);
        }

        private SyntaxNode WrapParmeterToNewLine(SyntaxNode node, SyntaxTriviaList leadingWhitespace)
        {
            var leadingTrivia = node.HasLeadingTrivia ?
                leadingWhitespace :
                leadingWhitespace.Insert(0, SyntaxFactory.ElasticEndOfLine(Environment.NewLine));

            return node
                .NormalizeWhitespace()
                .WithLeadingTrivia(leadingTrivia);
        }
    }
}
