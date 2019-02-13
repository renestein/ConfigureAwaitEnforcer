using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

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
                                                                                    Resources.ResourceManager,
                                                                                    typeof(Resources));

    private static readonly LocalizableString MessageFormat =
      new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat),
                                    Resources.ResourceManager,
                                    typeof(Resources));

    private static readonly LocalizableString Description =
      new LocalizableResourceString(nameof(Resources.AnalyzerDescription),
                                    Resources.ResourceManager,
                                    typeof(Resources));

    private static readonly DiagnosticDescriptor RULE = new DiagnosticDescriptor(DiagnosticId,
                                                                                 Title,
                                                                                 MessageFormat,
                                                                                 Category,
                                                                                 ConfigureAwaitEnforcerOptions
                                                                                   .Default.Severity,
                                                                                 true,
                                                                                 Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RULE);

    public override void Initialize(AnalysisContext context)
    {
      context.RegisterSyntaxNodeAction(AnalyzeAwait,
                                       SyntaxKind.AwaitExpression);
    }

    private static void AnalyzeAwait(SyntaxNodeAnalysisContext context)
    {
      var currentAwait = (AwaitExpressionSyntax) context.Node;
      var semanticModel = context.SemanticModel;

      var hasConfigureAwait = currentAwait
                              .DescendantTokens()
                              .Any(token => token.Value != null &&
                                            token.Value.ToString().Equals(CONFIGUREAWAIT_METHOD_NAME,
                                                                          StringComparison.Ordinal));

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

    private static bool canUseConfigureAwaitMethod(AwaitExpressionInfo awaitExpression)
    {
      var containingTypeName = awaitExpression.GetResultMethod?.ContainingType?.Name;
      return containingTypeName?.StartsWith(nameof(TaskAwaiter),
                                            StringComparison.OrdinalIgnoreCase) ?? false;
    }
  }
}