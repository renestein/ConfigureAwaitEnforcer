using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace ConfigureAwaitEnforcer
{
  [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConfigureAwaitEnforcerCodeFixProvider)), Shared]
  public class ConfigureAwaitEnforcerCodeFixProvider : CodeFixProvider
  {
    private const string title = "Add ConfigureAwait(false)";

    public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ConfigureAwaitEnforcerAnalyzer.DiagnosticId);

    public sealed override FixAllProvider GetFixAllProvider()
    {
      // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
      return WellKnownFixAllProviders.BatchFixer;
    }

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
      var root = await context.Document
        .GetSyntaxRootAsync(context.CancellationToken)
        .ConfigureAwait(false);

      var diagnostic = context.Diagnostics.First();
      var diagnosticSpan = diagnostic.Location.SourceSpan;

      var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<AwaitExpressionSyntax>().First();

      // Register a code action that will invoke the fix.
      context.RegisterCodeFix(
          CodeAction.Create(
              title: title,
              createChangedSolution: c => addConfigureAwaitFalseNode(context.Document,
                                                                    declaration,
                                                                     c,
                                                                     context),
              equivalenceKey: title),
          diagnostic);
    }

    private async Task<Solution> addConfigureAwaitFalseNode(Document document,
                                                            AwaitExpressionSyntax awaitExpression,
                                                            CancellationToken cancellationToken,
                                                            CodeFixContext context)
    {
      var configureAwaitId = SyntaxFactory.IdentifierName(ConfigureAwaitEnforcerAnalyzer.CONFIGUREAWAIT_METHOD_NAME);
      var dot = SyntaxFactory.Token(SyntaxKind.DotToken);

      var callMethodConfigureAwait = SyntaxFactory.MemberAccessExpression(
        SyntaxKind.SimpleMemberAccessExpression,
        awaitExpression.Expression, dot, configureAwaitId);

      var awaitFalseArg = SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression));
      var configureAwaitMethodArgs = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[] { awaitFalseArg }));
      var invokeConfigureAwait = SyntaxFactory.InvocationExpression(callMethodConfigureAwait,
        configureAwaitMethodArgs);

      var awaitWithConfigureAwaitCall = awaitExpression.WithExpression(invokeConfigureAwait);

      var formattedAwaitWithConfigureAwaitCall = awaitWithConfigureAwaitCall.WithAdditionalAnnotations(Formatter.Annotation);
      var currentDocumentRoot = await document
        .GetSyntaxRootAsync(cancellationToken)
        .ConfigureAwait(false);

      return document
        .WithSyntaxRoot(currentDocumentRoot.ReplaceNode(awaitExpression,
          formattedAwaitWithConfigureAwaitCall))
        .Project
        .Solution;
    }
  }
}
