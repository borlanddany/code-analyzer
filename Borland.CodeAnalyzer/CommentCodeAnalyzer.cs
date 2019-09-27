using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Borland.CodeAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CommentCodeAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString Title = nameof(Resources.CommentCodeTitle).GetLocalizableString();
        private static readonly LocalizableString MessageFormat = nameof(Resources.CommentCodeMessageFormat).GetLocalizableString();
        private static readonly LocalizableString Description = nameof(Resources.CommentCodeDescription).GetLocalizableString();
        private static readonly DiagnosticDescriptor DiagnosticRule = new DiagnosticDescriptor(
            AnalyzerCodes.DiagnosticIds.CommentCode, Title, MessageFormat, AnalyzerCodes.Categories.Commentary,
            DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticRule);

        public override void Initialize(AnalysisContext context)
            => context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);

        private static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            var root = context.Tree.GetCompilationUnitRoot(context.CancellationToken);

            var comments = GetCommentsWithCode(root, context.CancellationToken);
            var commentsBlocks = GroupTriviasByBlock(root, comments);
            var blocksLocations = GetTriviasBlocksLocation(context.Tree, commentsBlocks);
            blocksLocations.ForEach(x =>
            {
                var diagnostic = Diagnostic.Create(DiagnosticRule, x);
                context.ReportDiagnostic(diagnostic);
            });
        }

        private static bool CheckTriviaLocateTogether(CompilationUnitSyntax root, SyntaxTrivia first, SyntaxTrivia second)
        {
            var trivia = root.FindTrivia(first.Span.End + 1);
            while (trivia.Span.End < second.Span.Start)
            {
                if (!(trivia.IsKind(SyntaxKind.WhitespaceTrivia) ||
                    trivia.IsKind(SyntaxKind.EndOfLineTrivia)))
                    return false;
                trivia = root.FindTrivia(trivia.Span.End + 1);
            }
            return true;
        }

        private static IEnumerable<SyntaxTrivia> GetCommentsWithCode(CompilationUnitSyntax root, CancellationToken cancellationToken)
        {
            var commentTrivias = root.DescendantTrivia()
                .Where(x => x.IsKind(SyntaxKind.MultiLineCommentTrivia) ||
                            x.IsKind(SyntaxKind.SingleLineCommentTrivia))
                .ToArray();
            var comments = commentTrivias
                .Select(GetCommentText)
                .ToList();

            return GetCommentsWithCodeIndexes(comments, cancellationToken)
                .Select(x => commentTrivias[x]);
        }

        private static IEnumerable<ICollection<SyntaxTrivia>> GroupTriviasByBlock(CompilationUnitSyntax root, IEnumerable<SyntaxTrivia> trivias)
        {
            var result = new List<List<SyntaxTrivia>>();
            return trivias.Aggregate(result, (x, y) => Aggregate(root, x, y));
        }

        private static IEnumerable<Location> GetTriviasBlocksLocation(SyntaxTree tree, IEnumerable<ICollection<SyntaxTrivia>> triviasBlocks)
            => triviasBlocks.Select(x => Location.Create(tree, TextSpan.FromBounds(x.First().Span.Start, x.Last().Span.End)));

        private static List<List<SyntaxTrivia>> Aggregate(CompilationUnitSyntax root, List<List<SyntaxTrivia>> seed, SyntaxTrivia trivia)
        {
            if (!seed.Any())
            {
                seed.Add(new List<SyntaxTrivia> { trivia });
                return seed;
            }
            if (CheckTriviaLocateTogether(root, seed.Last().Last(), trivia))
                seed.Last().Add(trivia);
            else
                seed.Add(new List<SyntaxTrivia> { trivia });
            return seed;
        }

        private static IEnumerable<int> GetCommentsWithCodeIndexes(IList<string> comments, CancellationToken cancellationToken)
        {
            var builder = new StringBuilder();
            var result = new List<int>();
            var checkBeforeIndex = true;
            var indexLimit = comments.Count;
            for (var i = 0; i < indexLimit; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException(cancellationToken);
                if (CheckComment(comments[i], cancellationToken))
                    result.Add(i);
                var enumerable = TryToCombineComments(i, checkBeforeIndex,
                    indexLimit, comments, builder, cancellationToken);
                result.AddRange(enumerable);
                var oldIndex = i;
                i = GetFirstDifferenceInteger(result, comments.Count);
                if (oldIndex >= i)
                    i = oldIndex + 1;
                indexLimit = GetLastDifferenceInteger(result, comments.Count);
                if (checkBeforeIndex)
                    checkBeforeIndex = false;
            }
            return result.Distinct().OrderBy(x => x);
        }

        private static IEnumerable<int> TryToCombineComments(int index, bool checkBeforeIndex,
            int indexLimit, ICollection<string> comments, StringBuilder builder, CancellationToken cancellationToken)
        {
            if (checkBeforeIndex)
                for (var i = 0; i < index; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(cancellationToken);
                    comments.Skip(i).Take(index - i + 1).ForEach(x => builder.AppendLine(x));
                    var combinedComment = builder.ToString();
                    builder.Clear();
                    if (CheckComment(combinedComment, cancellationToken))
                        yield return i;
                }
            if (index == comments.Count - 1) yield break;
            for (var i = index; i < indexLimit; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException(cancellationToken);
                comments.Skip(index).Take(comments.Count - i).ForEach(x => builder.AppendLine(x));
                var combinedComment = builder.ToString();
                builder.Clear();
                if (CheckComment(combinedComment, cancellationToken))
                {
                    for (var j = index + 1; j < comments.Count - i; j++)
                        yield return j;
                    yield break;
                }
            }
        }

        private static string GetCommentText(SyntaxTrivia trivia)
        {
            switch (trivia.Kind())
            {
                case SyntaxKind.SingleLineCommentTrivia:
                    return trivia.ToString().TrimStart('/').Trim(' ');
                case SyntaxKind.MultiLineCommentTrivia:
                    var text = trivia.ToString();
                    return text.Substring(2, text.Length - 4).Trim(' ');
            }
            return null;
        }

        private static bool CheckComment(string comment, CancellationToken cancellationToken)
        {
            var tree = CSharpSyntaxTree.ParseText(comment, cancellationToken: cancellationToken);
            var root = tree.GetCompilationUnitRoot(cancellationToken);
            return root.Members.Any() && root.Members.All(x => x.Kind() != SyntaxKind.IncompleteMember);
        }

        private static int GetFirstDifferenceInteger(IEnumerable<int> enumerable, int limit)
        {
            var range = Enumerable.Range(0, limit);
            var orderedEnumerable = enumerable.OrderBy(x => x);
            using (IEnumerator<int> rangeEnumerator = range.GetEnumerator(),
                orderedEnumerableEnumerator = orderedEnumerable.GetEnumerator())
            {
                while (orderedEnumerableEnumerator.MoveNext() && rangeEnumerator.MoveNext())
                    if (orderedEnumerableEnumerator.Current != rangeEnumerator.Current)
                        return rangeEnumerator.Current;
                return rangeEnumerator.Current + 1;
            }
        }

        private static int GetLastDifferenceInteger(ICollection<int> collection, int limit)
        {
            var range = Enumerable.Range(0, limit).Reverse();
            var enumerable = collection.OrderByDescending(x => x);
            using (IEnumerator<int> rangeEnumerator = range.GetEnumerator(),
                enumerableEnumerator = enumerable.GetEnumerator())
            {
                while (enumerableEnumerator.MoveNext() && rangeEnumerator.MoveNext())
                    if (enumerableEnumerator.Current != rangeEnumerator.Current)
                        return rangeEnumerator.Current;
                return rangeEnumerator.Current - 1;
            }
        }
    }
}
