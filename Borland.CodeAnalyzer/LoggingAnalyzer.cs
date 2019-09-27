using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Borland.CodeAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class LoggingAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString Title = nameof(Resources.LoggingTitle).GetLocalizableString();
        private static readonly LocalizableString MessageFormat = nameof(Resources.LoggingMessageFormat).GetLocalizableString();
        private static readonly LocalizableString Description = nameof(Resources.LoggingDescription).GetLocalizableString();
        private static readonly DiagnosticDescriptor DiagnosticRule = new DiagnosticDescriptor(
            AnalyzerCodes.DiagnosticIds.Logging, Title, MessageFormat, AnalyzerCodes.Categories.Logging,
            DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticRule);

        public override void Initialize(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, SyntaxKind.InvocationExpression);

        private static void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
        {
            var @namespace = typeof(Console).Namespace;
            var console = nameof(Console);
            var writeLine = nameof(Console.WriteLine);
            var write = nameof(Console.Write);

            if (CheckAndReport(context, writeLine, console, @namespace)) return;
            if (CheckAndReport(context, write, console, @namespace)) return;
        }

        private static bool CheckAndReport(SyntaxNodeAnalysisContext context, string method, string @class, string @namespace)
        {
            var invocationExpression = (InvocationExpressionSyntax)context.Node;
            var result = CheckForMethodCall(invocationExpression, method, @class,
                @namespace, context.CancellationToken);
            if (result)
            {
                var diagnostic = Diagnostic.Create(DiagnosticRule, context.Node.GetLocation(), $"{@class}.{method}");
                context.ReportDiagnostic(diagnostic);
            }
            return result;
        }

        private static bool CheckForMethodCall(InvocationExpressionSyntax invocationExpression, string method,
            string @class, string @namespace, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException(cancellationToken);
            switch (invocationExpression.Expression)
            {
                case IdentifierNameSyntax identifierName:
                    return CheckStaticMethodCall(identifierName, method, @class, @namespace, cancellationToken);
                case MemberAccessExpressionSyntax memberAccessExpression:
                    return CheckInstanceMethodCall(memberAccessExpression, method,
                        @class, @namespace, cancellationToken);
                default:
                    break;
            }
            return false;
        }

        private static bool CheckStaticMethodCall(IdentifierNameSyntax identifierName, string method,
            string @class, string @namespace, CancellationToken cancellationToken)
        {
            if (identifierName.Identifier.Text == method)
            {
                var namespaces = @namespace.Split('.');
                return identifierName.SyntaxTree.GetCompilationUnitRoot(cancellationToken)
                    .Usings
                    .Where(x => x.StaticKeyword.IsKind(SyntaxKind.StaticKeyword))
                    .Select(x => x.Name)
                    .OfType<QualifiedNameSyntax>()
                    .Where(x => x.Right.Identifier.Text == @class)
                    .Any(x => CheckNamespace(x.Left, namespaces, namespaces.Length - 1, cancellationToken));
            }
            return false;
        }

        private static bool CheckInstanceMethodCall(MemberAccessExpressionSyntax expression,
            string method, string @class, string @namespace, CancellationToken cancellationToken)
        {
            if (expression.Name.Identifier.Text == method)
            {
                switch (expression.Expression)
                {
                    case IdentifierNameSyntax identifierName:
                        if (identifierName.Identifier.Text == @class)
                            return true;
                        var alias = expression.SyntaxTree
                            .GetCompilationUnitRoot(cancellationToken)
                            .Usings
                            .Where(x => x.Alias != null)
                            .Where(x => x.Alias.Name.Identifier.Text == identifierName.Identifier.Text)
                            .Select(x => x.Name)
                            .OfType<QualifiedNameSyntax>()
                            .SingleOrDefault();
                        if (alias != null && alias.Right.Identifier.Text == @class)
                        {
                            var namespaces = @namespace.Split('.');
                            return CheckNamespace(alias.Left, namespaces, namespaces.Length - 1, cancellationToken);
                        }
                        break;
                    case MemberAccessExpressionSyntax memberAccessExpression:
                        if (memberAccessExpression.Name.Identifier.Text == @class)
                        {
                            var namespaces = @namespace.Split('.');
                            return CheckNamespace(memberAccessExpression.Expression, namespaces, namespaces.Length - 1, cancellationToken);
                        }
                        break;
                    default:
                        break;
                }
            }
            return false;
        }

        private static bool CheckNamespace(ExpressionSyntax expression, string[] namespaces, int index,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException(cancellationToken);
            if (index < 0)
                return false;
            switch (expression)
            {
                case IdentifierNameSyntax identifierName:
                    if (identifierName.Identifier.Text == namespaces[index] && index == 0)
                        return true;
                    var alias = expression.SyntaxTree
                        .GetCompilationUnitRoot(cancellationToken)
                        .Usings
                        .Where(x => x.Alias != null)
                        .SingleOrDefault(x => x.Alias.Name.Identifier.Text == identifierName.Identifier.Text);
                    if (alias != null)
                        return CheckNamespace(alias.Name, namespaces, index, cancellationToken);
                    break;
                case AliasQualifiedNameSyntax aliasQualifiedName:
                    if (aliasQualifiedName.Name.Identifier.Text == namespaces[index])
                    {
                        var aliasName = aliasQualifiedName.Alias.Identifier.Text;
                        if (aliasName == "global" && index == 0)
                            return true;
                        return CheckNamespace(aliasQualifiedName.Alias, namespaces, index, cancellationToken);
                    }
                    break;
                case MemberAccessExpressionSyntax memberAccessExpression:
                    if (memberAccessExpression.Name.Identifier.Text == namespaces[index])
                        return CheckNamespace(memberAccessExpression.Expression, namespaces, index--, cancellationToken);
                    break;
                default:
                    break;
            }
            return false;
        }
    }
}
