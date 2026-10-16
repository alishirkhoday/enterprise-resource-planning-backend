using ERP.Application.Common.Interfaces.Branches;
using ERP.Infrastructure.MainDatabase.Context;
using Microsoft.EntityFrameworkCore;

namespace ERP.Test.xUnit.Infrastructure
{
    /// <summary>
    /// The in-memory provider will happily evaluate anything in C#, so passing those tests says
    /// nothing about whether a real database can do the filtering. These inspect the SQL that would
    /// be sent, which needs no connection, and hold the filter to two things: it reaches the server,
    /// and the caller's access arrives as a parameter rather than baked into the text.
    /// </summary>
    public sealed class BranchFilterTranslationTests
    {
        private sealed record TestBranchContext(
            Guid? CurrentBranchId,
            IReadOnlyCollection<Guid> AccessibleBranchIds,
            bool CanAccessAllBranches) : IBranchContext;

        private static MainDbContext CreateContext(IBranchContext branchContext) =>
            new(new DbContextOptionsBuilder<MainDbContext>()
                    .UseSqlServer("Server=localhost;Database=Translation;Trusted_Connection=True;")
                    .Options,
                branchContext);

        [Fact]
        public void The_branch_condition_is_sent_to_the_database()
        {
            var branch = Guid.NewGuid();
            using var context = CreateContext(new TestBranchContext(branch, [branch], CanAccessAllBranches: false));

            var sql = context.Customers.ToQueryString();

            Assert.Contains("BranchId", sql, StringComparison.Ordinal);
            Assert.Contains("IsDeleted", sql, StringComparison.Ordinal);
        }

        /// <summary>
        /// The access level must not be baked into the statement. A literal would mean the SQL -
        /// and the query plan the server caches for it - belonged to whoever ran it first.
        /// <para>
        /// Only the statement itself is inspected. <c>ToQueryString</c> prints a DECLARE preamble
        /// with the parameter values so the text can be pasted into a query window, and reading the
        /// value there proves nothing either way.
        /// </para>
        /// </summary>
        [Fact]
        public void The_callers_access_arrives_as_a_parameter()
        {
            var branch = Guid.Parse("11111111-1111-1111-1111-111111111111");
            using var context = CreateContext(new TestBranchContext(branch, [branch], CanAccessAllBranches: false));

            var statement = StatementOf(context.Customers.ToQueryString());

            Assert.DoesNotContain("11111111-1111-1111-1111-111111111111", statement, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("@", statement, StringComparison.Ordinal);
        }

        /// <summary>
        /// The statement without the DECLARE preamble that ToQueryString puts in front of it.
        /// </summary>
        private static string StatementOf(string queryString)
        {
            var start = queryString.IndexOf("SELECT", StringComparison.Ordinal);
            return start < 0 ? queryString : queryString[start..];
        }

        /// <summary>
        /// Two contexts built one after the other must produce SQL carrying their own values. If
        /// the model had captured the first instance, the second would repeat the first's.
        /// </summary>
        [Fact]
        public void Two_callers_do_not_share_one_compiled_filter()
        {
            var first = Guid.NewGuid();
            var second = Guid.NewGuid();

            string firstSql;
            using (var context = CreateContext(new TestBranchContext(first, [first], CanAccessAllBranches: false)))
            {
                firstSql = context.Customers.ToQueryString();
            }

            string secondSql;
            using (var context = CreateContext(new TestBranchContext(second, [second], CanAccessAllBranches: true)))
            {
                secondSql = context.Customers.ToQueryString();
            }

            // One cached model serves both, so the statement itself is identical.
            Assert.Equal(StatementOf(firstSql), StatementOf(secondSql));

            // Only the parameter values differ, because they are read from the context per query.
            Assert.NotEqual(firstSql, secondSql);
        }

        [Fact]
        public void An_entity_without_a_branch_is_filtered_only_on_soft_delete()
        {
            using var context = CreateContext(new TestBranchContext(null, [], CanAccessAllBranches: false));

            var statement = StatementOf(context.CustomerGroups.ToQueryString());

            Assert.Contains("IsDeleted", statement, StringComparison.Ordinal);
            Assert.DoesNotContain("BranchId", statement, StringComparison.Ordinal);
        }
    }
}
