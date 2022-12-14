using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MakeSafeCast
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeSafeCastCodeFixProvider)), Shared]
    public class MakeSafeCastCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(MakeSafeCastAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<LocalDeclarationStatementSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.CodeFixTitle,
                    createChangedDocument: c => MakeSafeCastAsync(context.Document, declaration, c),
                    equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                diagnostic);
        }

        private async Task<Document> MakeSafeCastAsync(Document document, LocalDeclarationStatementSyntax localDeclaration, CancellationToken cancellationToken)
        {
            // use the same local declaration that is given
            // first goal is just change the right side to fido as Dog instead of (Dog)fido
            // then figure out how to make type on the left side nullable
            var identifier = localDeclaration.Declaration.Variables.First().Identifier;
            var castType = ((CastExpressionSyntax)localDeclaration.Declaration.Variables.First().Initializer.Value).Type;
            var expression = ((CastExpressionSyntax)localDeclaration.Declaration.Variables.First().Initializer.Value).Expression;
            var variables = localDeclaration.Declaration.Variables;
            //TODO: change variables.First() to use as keyword

            LocalDeclarationStatementSyntax newDeclaration = SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.NullableType(castType, SyntaxFactory.Token(SyntaxKind.QuestionToken)),
                    variables
                    // second arg:
                    //SeparatedSyntaxList<Microsoft.CodeAnalysis.CSharp.Syntax.VariableDeclaratorSyntax> variables
                )
            );
            // nullable_cast_type identifier equals_token variable as_token cast_type

            SyntaxNode oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            SyntaxNode newRoot = oldRoot.ReplaceNode(localDeclaration, newDeclaration);

            return document.WithSyntaxRoot(newRoot);
        }
    }
}
