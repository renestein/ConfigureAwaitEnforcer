using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
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

    private const string Category = "ConfigureAwait";
    protected internal const string CONFIGUREAWAIT_METHOD_NAME = "ConfigureAwait";

    private static readonly DiagnosticDescriptor RULE = new DiagnosticDescriptor(DiagnosticId,
      Title,
      MessageFormat,
      Category,
      DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RULE);

    public override void Initialize(AnalysisContext context)
    {
      context.RegisterSyntaxNodeAction(AnalyzeSymbol,
        SyntaxKind.AwaitExpression);
    }

    private static void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
    {
      var currentAwait = (AwaitExpressionSyntax)context.Node;

      var hasConfigureAwait = currentAwait
        .DescendantTokens()
        .Any(token => token.Value != null &&
                      token.Value.ToString().Equals(CONFIGUREAWAIT_METHOD_NAME,
                                                    StringComparison.Ordinal));

      if (!hasConfigureAwait)
      {
        var diagnostic = Diagnostic.Create(RULE, currentAwait.GetLocation());
        context.ReportDiagnostic(diagnostic);
      }

    }
  }
}
