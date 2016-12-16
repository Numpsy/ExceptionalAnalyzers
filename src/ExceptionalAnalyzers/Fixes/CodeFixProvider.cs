using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Exceptional.Analyzers.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;

namespace Exceptional.Analyzers.Fixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ExceptionalAnalyzersCodeFixProvider)), Shared]
    public class ExceptionalAnalyzersCodeFixProvider : CodeFixProvider
    {
        private const string title = "Make uppercase";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ThrowSiteAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var throwSite = root.FindToken(diagnosticSpan.Start);
            var member = FindMember(throwSite.Parent);

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedSolution: c => AddMissingExceptionDocumentation(context, member, c),
                    equivalenceKey: title),
                diagnostic);
        }

        private async Task<Solution> AddMissingExceptionDocumentation(CodeFixContext context, MemberDeclarationSyntax member, CancellationToken cancellationToken)
        {
            if (member is MethodDeclarationSyntax)
            {
                var method = (MethodDeclarationSyntax)member;
                
            }

            return context.Document.Project.Solution; 
        }

        private MemberDeclarationSyntax FindMember(SyntaxNode throwSiteNode)
        {
            var currentParent = throwSiteNode.Parent;
            while (currentParent != null)
            {
                if (currentParent is MemberDeclarationSyntax)
                    return (MemberDeclarationSyntax)currentParent;
                currentParent = currentParent.Parent;
            }
            return null;
        }

        private async Task<Solution> MakeUppercaseAsync(Document document, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        {
            // Compute new uppercase name.
            var identifierToken = typeDecl.Identifier;
            var newName = identifierToken.Text.ToUpperInvariant();

            // Get the symbol representing the type to be renamed.
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var typeSymbol = semanticModel.GetDeclaredSymbol(typeDecl, cancellationToken);

            // Produce a new solution that has all references to that type renamed, including the declaration.
            var originalSolution = document.Project.Solution;
            var optionSet = originalSolution.Workspace.Options;
            var newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, typeSymbol, newName, optionSet, cancellationToken).ConfigureAwait(false);

            // Return the new solution with the now-uppercase type name.
            return newSolution;
        }
    }
}