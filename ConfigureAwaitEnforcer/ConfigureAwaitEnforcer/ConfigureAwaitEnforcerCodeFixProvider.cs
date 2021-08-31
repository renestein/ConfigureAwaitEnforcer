using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace ConfigureAwaitEnforcer
{
  [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConfigureAwaitEnforcerCodeFixProvider))]
  [Shared]
  public class ConfigureAwaitEnforcerCodeFixProvider : CodeFixProvider
  {
    private const string AWAIT_FALSE_TITLE = "Add ConfigureAwait(false).";
    private const string AWAIT_TRUE_TITLE = "Add ConfigureAwait(true).";

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
      ImmutableArray.Create(ConfigureAwaitEnforcerAnalyzer.DiagnosticId);

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

      SyntaxNode declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf()
                                   .OfType<AwaitExpressionSyntax>()
                                   .FirstOrDefault() ??

                                   (SyntaxNode)root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf()
                                                .OfType<ForEachStatementSyntax>()
                                                .FirstOrDefault() ??

                               root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf()
                                   .OfType<UsingStatementSyntax>()
                                   .FirstOrDefault()?.Declaration?.Variables.First() ??

                               (SyntaxNode)root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf()
                                   .OfType<UsingStatementSyntax>()
                                   .FirstOrDefault()?.DescendantNodes().FirstOrDefault(node => node.IsKind(SyntaxKind.IdentifierName)) ??

                               root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf()
                                   .OfType<LocalDeclarationStatementSyntax>()
                                   .First()
                                   .Declaration.Variables.First();


      // Register a code action that will invoke the fix.
      context.RegisterCodeFix(CodeAction.Create(AWAIT_FALSE_TITLE,
                                                c => addConfigureAwaitNode(context.Document,
                                                                           declaration,
                                                                           c,
                                                                           false),
                                                AWAIT_FALSE_TITLE),
                              diagnostic);

      context.RegisterCodeFix(CodeAction.Create(AWAIT_TRUE_TITLE,
                                                c => addConfigureAwaitNode(context.Document,
                                                                           declaration,
                                                                           c,
                                                                           true),
                                                AWAIT_TRUE_TITLE),
                              diagnostic);
    }

    private async Task<Solution> addConfigureAwaitNode(Document document,
                                                       SyntaxNode awaitSyntaxNode,
                                                       CancellationToken cancellationToken,
                                                       bool configureAwaitValue)
    {
      if (awaitSyntaxNode is VariableDeclaratorSyntax usingVariable)
      {
        return await addConfigureAwaitForUsingInitializer(document, cancellationToken, usingVariable, configureAwaitValue)
                    .ConfigureAwait(false);
      }

      var awaitNode = awaitSyntaxNode as AwaitExpressionSyntax;
      var forEachNode = awaitSyntaxNode as ForEachStatementSyntax;
      var identifierNameSyntax = awaitSyntaxNode as IdentifierNameSyntax;
      var configureAwaitId = SyntaxFactory.IdentifierName(ConfigureAwaitEnforcerAnalyzer.CONFIGUREAWAIT_METHOD_NAME);
      var dot = SyntaxFactory.Token(SyntaxKind.DotToken);


      var callMethodConfigureAwait = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                                          awaitNode?.Expression ?? forEachNode?.Expression ?? identifierNameSyntax,
                                                                          dot,
                                                                          configureAwaitId);

      var awaitArg = SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(configureAwaitValue
                                                                              ? SyntaxKind.TrueLiteralExpression
                                                                              : SyntaxKind.FalseLiteralExpression));

      var configureAwaitMethodArgs = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[] { awaitArg }));
      var invokeConfigureAwait = SyntaxFactory.InvocationExpression(callMethodConfigureAwait,
                                                                    configureAwaitMethodArgs);



      SyntaxNode awaitWithConfigureAwaitCall;

      if (awaitNode != null)
      {
        awaitWithConfigureAwaitCall = awaitNode.WithExpression(invokeConfigureAwait);
      }
      else if (forEachNode != null)
      {
        awaitWithConfigureAwaitCall = forEachNode.WithExpression(invokeConfigureAwait);
      }
      else
      {
        awaitWithConfigureAwaitCall = invokeConfigureAwait;
      }

      var formattedAwaitWithConfigureAwaitCall = awaitWithConfigureAwaitCall.WithAdditionalAnnotations(Formatter.Annotation);
      var currentDocumentRoot = await getDocumentRoot(document, cancellationToken).ConfigureAwait(false);


      return document.WithSyntaxRoot(currentDocumentRoot.ReplaceNode(awaitSyntaxNode,
                                                                     formattedAwaitWithConfigureAwaitCall))
                     .Project
                     .Solution;
    }

    private static async Task<SyntaxNode> getDocumentRoot(Document document,
                                                          CancellationToken cancellationToken)
    {
      var currentDocumentRoot = await document
                                      .GetSyntaxRootAsync(cancellationToken)
                                      .ConfigureAwait(false);
      return currentDocumentRoot;
    }

    private async Task<Solution> addConfigureAwaitForUsingInitializer(Document document,
                                                                 CancellationToken cancellationToken,
                                                                 VariableDeclaratorSyntax usingVariable,
                                                                 bool configureAwaitValue)
    {

      var initializer = usingVariable.Initializer;

      var newInitializer = SyntaxFactory.EqualsValueClause(
                                                 SyntaxFactory.ParseExpression($"{initializer.Value}.{ConfigureAwaitEnforcerAnalyzer.CONFIGUREAWAIT_METHOD_NAME}({configureAwaitValue.ToString().ToLowerInvariant()})")
                                                              .WithAdditionalAnnotations(Formatter.Annotation));

      var currentDocumentRoot = await getDocumentRoot(document, cancellationToken).ConfigureAwait(false);

      return document.WithSyntaxRoot(currentDocumentRoot.ReplaceNode(initializer,
                                                                     newInitializer))
                     .Project
                     .Solution;

    }
  }
}