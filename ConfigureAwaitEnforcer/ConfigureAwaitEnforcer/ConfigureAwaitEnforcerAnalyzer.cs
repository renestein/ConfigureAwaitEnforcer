using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Runtime.CompilerServices;

namespace ConfigureAwaitEnforcer
{
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class ConfigureAwaitEnforcerAnalyzer : DiagnosticAnalyzer
  {
    public const string DiagnosticId = "ConfigureAwaitEnforcer";

    private const string Category = "ConfigureAwait";
    protected internal const string CONFIGUREAWAIT_METHOD_NAME = "ConfigureAwait";

    // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
    // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle),
      Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString MessageFormat =
      new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager,
        typeof(Resources));

    private static readonly LocalizableString Description =
      new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager,
        typeof(Resources));

#pragma warning disable RS2000 // Add analyzer diagnostic IDs to analyzer release.
    private static readonly DiagnosticDescriptor RULE = new DiagnosticDescriptor(DiagnosticId,
#pragma warning restore RS2000 // Add analyzer diagnostic IDs to analyzer release.
      Title,
      MessageFormat,
      Category,
      Config.Default.Severity,
      true,
      Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RULE);

    public override void Initialize(AnalysisContext context)
    {
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
      context.EnableConcurrentExecution();
      context.RegisterSyntaxNodeAction(AnalyzeAwait,
        SyntaxKind.AwaitExpression,
                      SyntaxKind.ForEachStatement,
                      SyntaxKind.UsingStatement,
                      SyntaxKind.LocalDeclarationStatement);
    }

    private static void AnalyzeAwait(SyntaxNodeAnalysisContext context)
    {
      switch (context.Node)
      {
        case AwaitExpressionSyntax _:
          {
            analyzeAwaitExpression(context);
            break;
          }
        case ForEachStatementSyntax _:
          {
            analyzeForEachAwait(context);
            break;
          }
        case LocalDeclarationStatementSyntax _:
          {
            analyzeLocalDeclarationStatementAwait(context);
            break;
          }
        case UsingStatementSyntax _:
          {
            analyzeUsingAwait(context);
            break;
          }
      }

    }

    private static void analyzeAwaitExpression(SyntaxNodeAnalysisContext context)
    {
      var currentAwait = (AwaitExpressionSyntax)context.Node;

      var semanticModel = context.SemanticModel;

      var hasConfigureAwait = currentAwait
                              .DescendantTokens()
                              .Any(token => token.Value != null &&
                                            token.Value.ToString().Equals(CONFIGUREAWAIT_METHOD_NAME,
                                                                          StringComparison.Ordinal) &&
                                            token.Parent.Equals(currentAwait));

      if (hasConfigureAwait)
      {
        return;
      }


      var awaitSemanticInfo = semanticModel.GetAwaitExpressionInfo(currentAwait);

      if (canUseConfigureAwaitMethod(awaitSemanticInfo))
      {
        var diagnostic = Diagnostic.Create(RULE, currentAwait.GetLocation());
        context.ReportDiagnostic(diagnostic);
      }
    }

    private static void analyzeLocalDeclarationStatementAwait(SyntaxNodeAnalysisContext context)
    {
      var localDeclarationStatementSyntax = (LocalDeclarationStatementSyntax)context.Node;
      if (localDeclarationStatementSyntax.AwaitKeyword.FullSpan.IsEmpty ||
          localDeclarationStatementSyntax.UsingKeyword.FullSpan.IsEmpty ||
          (!localDeclarationStatementSyntax.Declaration.Variables.Any()))
      {
        return;
      }


      var hasConfigureAwait = localDeclarationStatementSyntax
                              .Declaration
                              .DescendantTokens()
                              .Any(token => token.Value != null &&
                                            token.Value.ToString().Equals(CONFIGUREAWAIT_METHOD_NAME,
                                                                          StringComparison.Ordinal));
      if (hasConfigureAwait)
      {
        return;
      }

      var diagnostic = Diagnostic.Create(RULE, localDeclarationStatementSyntax.GetLocation());
      context.ReportDiagnostic(diagnostic);

    }

    private static void analyzeUsingAwait(SyntaxNodeAnalysisContext context)
    {
      var usingStatementNode = (UsingStatementSyntax)context.Node;
      
      if (usingStatementNode.AwaitKeyword.FullSpan.IsEmpty ||
          usingStatementNode.UsingKeyword.FullSpan.IsEmpty ||
          (usingStatementNode.Declaration == null ||        
          !usingStatementNode.Declaration.Variables.Any())
          && !usingStatementNode.DescendantTokens().Any(token => token.IsKind(SyntaxKind.IdentifierToken)))
      {
        return;
      }


      var hasConfigureAwait = usingStatementNode                             
                              .DescendantTokens()
                              .Any(token => token.Value != null &&
                                            token.Value.ToString().Equals(CONFIGUREAWAIT_METHOD_NAME,
                                                                          StringComparison.Ordinal));
      if (hasConfigureAwait)
      {
        return;
      }

      var diagnostic = Diagnostic.Create(RULE, usingStatementNode.GetLocation());
      context.ReportDiagnostic(diagnostic);

    }
    private static void analyzeForEachAwait(SyntaxNodeAnalysisContext context)
    {
      var forEachStatementSyntax = (ForEachStatementSyntax)context.Node;
      if (String.IsNullOrEmpty(forEachStatementSyntax.AwaitKeyword.FullSpan.ToString()))
      {
        return;
      }

      var semanticModel = context.SemanticModel;

      var forEachStatementInfo = semanticModel.GetForEachStatementInfo(forEachStatementSyntax);    

      var shouldAddConfigureAwait = forEachStatementInfo.IsAsynchronous &&
                                    forEachStatementInfo.GetEnumeratorMethod != null &&
                                    !forEachStatementSyntax.DescendantTokens().Any(token => token.ToString().Equals(CONFIGUREAWAIT_METHOD_NAME, StringComparison.Ordinal));

      if (!shouldAddConfigureAwait)
      {
        return;
      }

      var diagnostic = Diagnostic.Create(RULE, forEachStatementSyntax.GetLocation());
      context.ReportDiagnostic(diagnostic);

    }

    private static bool canUseConfigureAwaitMethod(AwaitExpressionInfo awaitExpression)
    {
      var containingTypeName = awaitExpression.GetResultMethod?.ContainingType?.Name;
      return (containingTypeName?.StartsWith(nameof(TaskAwaiter),
                                             StringComparison.OrdinalIgnoreCase) ?? false) ||
             (containingTypeName?.StartsWith(nameof(ValueTaskAwaiter<Object>)) ?? false);

    }
  }
}