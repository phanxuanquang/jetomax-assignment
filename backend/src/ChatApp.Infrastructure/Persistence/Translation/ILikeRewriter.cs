using System.Linq.Expressions;
using System.Reflection;
using ChatApp.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Infrastructure.Persistence.Translation;

/// <summary>
/// Rewrites calls to <see cref="IAppDbContext.ILike"/> in a query's expression tree into
/// <see cref="EF.Functions"/>-style <c>NpgsqlDbFunctionsExtensions.ILike</c> calls before the query
/// executes. Application composes predicates against the <c>IAppDbContext</c> interface (e.g.
/// <c>db.Conversations.Where(c =&gt; db.ILike(c.DisplayName, term))</c>), so the expression tree
/// carries <see cref="IAppDbContext"/>'s own <see cref="MethodInfo"/> for <c>ILike</c> — a symbol
/// Npgsql's query translator has never seen and cannot recognize. Rewriting the tree to call
/// Npgsql's own extension method directly reuses its already-registered, already-correct
/// translation rather than re-implementing SQL generation.
/// </summary>
internal sealed class ILikeRewriter : ExpressionVisitor
{
    private static readonly MethodInfo PortMethod = typeof(IAppDbContext).GetMethod(nameof(IAppDbContext.ILike))!;

    private static readonly MethodInfo NpgsqlILikeMethod = typeof(NpgsqlDbFunctionsExtensions).GetMethod(
        nameof(NpgsqlDbFunctionsExtensions.ILike), [typeof(DbFunctions), typeof(string), typeof(string)])!;

    private static readonly Expression FunctionsExpression = Expression.Property(null, typeof(EF), nameof(EF.Functions));

    public static Expression Rewrite(Expression expression) => new ILikeRewriter().Visit(expression);

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method == PortMethod)
        {
            var value = Visit(node.Arguments[0]);
            var pattern = Visit(node.Arguments[1]);
            return Expression.Call(NpgsqlILikeMethod, FunctionsExpression, value, pattern);
        }

        return base.VisitMethodCall(node);
    }
}
