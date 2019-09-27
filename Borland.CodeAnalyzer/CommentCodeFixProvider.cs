using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Borland.CodeAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CommentCodeFixProvider)), Shared]
    public class CommentCodeFixProvider : CodeFixProvider
    {
        private const string CodeFixActionTitle = "Remove comment";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(AnalyzerCodes.DiagnosticIds.CommentCode);

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.SingleOrDefault(x => x.Id == AnalyzerCodes.DiagnosticIds.CommentCode);
            if (diagnostic == null)
                return Task.CompletedTask;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixActionTitle,
                    createChangedSolution: c => RemoveCommentsAsync(context.Document, context.Span, c),
                    equivalenceKey: CodeFixActionTitle),
                diagnostic);
            return Task.CompletedTask;
        }

        private static async Task<Solution> RemoveCommentsAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = RemoveTrivia(root, span);
            return document.WithSyntaxRoot(newRoot).Project.Solution;
        }

        private static SyntaxNode RemoveTrivia(SyntaxNode root, TextSpan span)
        {
            var oldNode = root.FindNode(span);
            var oldLeadingTrivia = oldNode.GetLeadingTrivia();
            var newLeadingTrivia = CleanTrivias(oldLeadingTrivia, span);
            var oldTrailingTrivia = oldNode.GetTrailingTrivia();
            var newTrailingTrivia = CleanTrivias(oldTrailingTrivia, span);
            var newNode = oldNode
                .WithoutTrivia()
                .WithLeadingTrivia(newLeadingTrivia)
                .WithTrailingTrivia(newTrailingTrivia);
            return root.ReplaceNode(oldNode, newNode);
        }

        private static SyntaxTriviaList CleanTrivias(SyntaxTriviaList old, TextSpan span)
        {
            if (old.Span.Start > span.End || old.Span.End < span.Start) return old;
            var before = old
                .TakeWhile(x => x.Span.End < span.Start);
            var buffer = old
                .SkipWhile(x => x.Span.Start < span.End)
                .SkipWhile(x => x.IsKind(SyntaxKind.WhitespaceTrivia))
                .ToList();
            var after = buffer.Any() && buffer.First().IsKind(SyntaxKind.EndOfLineTrivia) ? buffer.Skip(1) : buffer;
            var list = SyntaxTriviaList.Empty
                .AddRange(before)
                .AddRange(after);
            return list;
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }
    }
}
