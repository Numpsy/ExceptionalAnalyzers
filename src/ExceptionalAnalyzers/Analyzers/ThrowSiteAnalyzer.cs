using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Exceptional.Analyzers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Exceptional.Analyzers.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ThrowSiteAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ExceptionalThrowSiteAnalyzer";

        private static readonly LocalizableString Title =
            new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat =
            new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description =
            new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Method);
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Property);
        }

        private void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var syntaxTree = context.Symbol.DeclaringSyntaxReferences[0]?.GetSyntax(context.CancellationToken);
            if (syntaxTree is MethodDeclarationSyntax)
            {
                var throwStatements = FindThrowStatements(((MethodDeclarationSyntax)syntaxTree).Body?.Statements);
                AnalyzeThrowStatements(throwStatements, context, syntaxTree);
            }
            else if (syntaxTree is PropertyDeclarationSyntax)
            {
                foreach (var accessor in ((PropertyDeclarationSyntax)syntaxTree).AccessorList.Accessors)
                {
                    var throwStatements = FindThrowStatements(accessor.Body?.Statements);
                    AnalyzeThrowStatements(throwStatements, context, syntaxTree);
                }
            }
        }

        private IEnumerable<ThrowStatementSyntax> FindThrowStatements(IEnumerable<StatementSyntax> statements)
        {
            foreach (var statement in statements)
            {
                if (statement is ThrowStatementSyntax)
                    yield return (ThrowStatementSyntax)statement;
                else
                {
                    foreach (var childStatement in FindThrowStatements(statement.ChildNodes().OfType<StatementSyntax>()))
                        yield return childStatement;
                }
            }
        }

        private void AnalyzeThrowStatements(IEnumerable<ThrowStatementSyntax> throwStatements, SymbolAnalysisContext context, SyntaxNode syntaxTree)
        {
            if (throwStatements != null && throwStatements.Any())
            {
                var symbolDocumentation = XmlSerialization.Deserialize<MethodDocumentation>(context.Symbol.GetDocumentationCommentXml());
                var semanticModel = context.Compilation.GetSemanticModel(syntaxTree.SyntaxTree);
                foreach (var throwStatement in throwStatements)
                {
                    var thrownExceptionType = semanticModel.GetTypeInfo(throwStatement.Expression).Type;

                    var isCatched = IsExceptionCatched(throwStatement, thrownExceptionType, semanticModel);
                    if (isCatched == false)
                    {
                        var isExceptionDocumented = IsExceptionDocumented(thrownExceptionType, symbolDocumentation, context);
                        if (isExceptionDocumented == false)
                        {
                            var diagnostic = Diagnostic.Create(Rule, throwStatement.GetLocation(), thrownExceptionType);
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }
        }

        private static bool IsExceptionDocumented(ITypeSymbol thrownExceptionType, MethodDocumentation symbolDocumentation, SymbolAnalysisContext context)
        {
            foreach (var documentedException in symbolDocumentation.Exceptions)
            {
                var documentedExceptionTypeName = documentedException.Type.Substring(2);
                var documentedExceptionType = context.Compilation.GetTypeByMetadataName(documentedExceptionTypeName);
                if (thrownExceptionType.Equals(documentedExceptionType))
                    return true;
            }
            return false;
        }

        private static bool IsExceptionCatched(SyntaxNode throwSiteNode, ITypeSymbol thrownExceptionType, SemanticModel semanticModel)
        {
            var currentParent = throwSiteNode.Parent;
            while (currentParent != null)
            {
                if (currentParent is TryStatementSyntax)
                {
                    foreach (var catches in ((TryStatementSyntax)currentParent).Catches)
                    {
                        var catchedType = semanticModel.GetTypeInfo(catches.Declaration.Type).Type;
                        if (catchedType.Equals(thrownExceptionType))
                            return true;
                    }
                }
                currentParent = currentParent.Parent;
            }
            return false;
        }
    }
}
