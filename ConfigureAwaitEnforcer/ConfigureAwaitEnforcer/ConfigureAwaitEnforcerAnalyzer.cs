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

    private static readonly DiagnosticDescriptor RULE = new DiagnosticDescriptor(DiagnosticId,
      Title,
      MessageFormat,
      Category,
      Config.Default.Severity,
      true,
      Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RULE);

    public override void Initialize(AnalysisContext context)
    {
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
          localDeclarationStatementSyntax.UsingKeyword.FullSpan.IsEmpty)
      {
        return;
      }


      var hasConfigureAwait = localDeclarationStatementSyntax
                              .DescendantTokens()
                              .Any(token => token.Value != null &&
                                            token.Value.ToString().Equals(CONFIGUREAWAIT_METHOD_NAME,
                                                                          StringComparison.Ordinal) && token.Parent.Equals(localDeclarationStatementSyntax));
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
          usingStatementNode.UsingKeyword.FullSpan.IsEmpty)
      {
        return;
      }


      var hasConfigureAwait = usingStatementNode
                              .DescendantTokens()
                              .Any(token => token.Value != null &&
                                            token.Value.ToString().Equals(CONFIGUREAWAIT_METHOD_NAME,
                                                                          StringComparison.Ordinal) && token.Parent.Equals(usingStatementNode));
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


      var hasConfigureAwait = forEachStatementSyntax
                              .DescendantTokens()
                              .Any(token => token.Value != null &&
                                            token.Value.ToString().Equals(CONFIGUREAWAIT_METHOD_NAME,
                                                                          StringComparison.Ordinal) && token.Parent.Equals(forEachStatementSyntax));
      if (hasConfigureAwait)
      {
        return;
      }

      var semanticModel = context.SemanticModel;
      var forEachInfo = semanticModel.GetForEachStatementInfo(forEachStatementSyntax);
      if (forEachInfo.GetEnumeratorMethod?.ReturnType.Name.StartsWith("IAsyncEnumerator") ?? false)
      {
        var diagnostic = Diagnostic.Create(RULE, forEachStatementSyntax.GetLocation());
        context.ReportDiagnostic(diagnostic);
      }
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