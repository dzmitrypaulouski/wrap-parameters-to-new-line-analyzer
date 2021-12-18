using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace WrapParameters
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class WrapParametersAnalyzer : DiagnosticAnalyzer
    {
        private const string Category = "Formatting";
        public const string DiagnosticId = "DPA0001";

        // TODO: Remove comments and move to separate file
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Rule);
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
        {
            var methodDeclaration = (MethodDeclarationSyntax)context.Node;
            var methodSymbol = (IMethodSymbol)context.ContainingSymbol;
            
            // TODO: Make configurable
            if (methodDeclaration.ParameterList.Parameters.Count < 3)
            {
                return;
            }

            var isFirstParameterWrapped = methodDeclaration
                .ParameterList
                .GetFirstToken()
                .TrailingTrivia
                .Any(trivia => trivia.IsKind(SyntaxKind.EndOfLineTrivia));
            
            var parametersCommaTokens = methodDeclaration
                .ParameterList
                .ChildTokens()
                .Where(t => t.IsKind(SyntaxKind.CommaToken));

            var areParametersWrappedToNewLines = parametersCommaTokens
                .Except(new[] { parametersCommaTokens.Last() })
                .All(ct => ct
                    .TrailingTrivia
                    .Any(trivia => trivia.IsKind(SyntaxKind.EndOfLineTrivia)));

            if (!(isFirstParameterWrapped && areParametersWrappedToNewLines))
            {
                var diagnostic = Diagnostic.Create(Rule, methodSymbol.Locations[0], methodSymbol.Name);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
