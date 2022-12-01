﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace MakeSafeCast
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MakeSafeCastAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "MakeSafeCast";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.LocalDeclarationStatement);
        }

        private static void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
        {
            // grab local declaration statements
            // filter to ones with CastExpression's on the right side
            var localDeclaration = (LocalDeclarationStatementSyntax)context.Node;    

            if (localDeclaration.Declaration.Variables.First().Initializer.Value.IsKind(SyntaxKind.CastExpression))
            {
                var diagnostic = Diagnostic.Create(Rule, localDeclaration.GetLocation(), $"{((CastExpressionSyntax)localDeclaration.Declaration.Variables.First().Initializer.Value).Type}");
                
                context.ReportDiagnostic(diagnostic);
            }

        }
    }
}
