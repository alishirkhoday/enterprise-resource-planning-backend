using ERP.Application.Common;
using ERP.Application.Common.Interfaces.Identity;
using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;
using ERP.Infrastructure.MainDatabase.Context;
using Microsoft.EntityFrameworkCore;

namespace ERP.Test.xUnit.Application
{
    /// <summary>
    /// Two things that were quietly not working: errors comparing by reference, so no request was
    /// ever answered with a 404, and audit columns that nothing ever filled in.
    /// </summary>
    public sealed class ErrorAndAuditingTests
    {
        private sealed record TestUser(Guid? UserId, string? UserName, bool IsAuthenticated) : ICurrentUser;

        private static CancellationToken Token => TestContext.Current.CancellationToken;

        private static MainDbContext CreateContext(string name, ICurrentUser actor) =>
            new(new DbContextOptionsBuilder<MainDbContext>()
                    .UseInMemoryDatabase(name)
                    .AddInterceptors(new AuditingInterceptor(actor))
                    .Options,
                UnrestrictedBranchContext.Instance);

        /// <summary>
        /// The bug this replaced: <c>Error</c> was a class, so comparing a result's error to a
        /// freshly built one compared references and was always false.
        /// </summary>
        [Fact]
        public void Two_errors_with_the_same_code_are_the_same_error()
        {
            Assert.Equal(Errors.General.NotFound(), Errors.General.NotFound());
            Assert.True(Errors.General.NotFound() == Errors.General.NotFound());
        }

        /// <summary>
        /// Messages carry identifiers and names, so two not-found errors rarely read alike. The
        /// code is what code branches on; the message is for a person.
        /// </summary>
        [Fact]
        public void The_code_decides_equality_not_the_message()
        {
            var forOneRecord = Errors.General.NotFound("42", "Account");
            var forAnother = Errors.General.NotFound("77", "Customer");

            Assert.NotEqual(forOneRecord.Message, forAnother.Message);
            Assert.Equal(forOneRecord, forAnother);
        }

        [Fact]
        public void Different_codes_are_different_errors()
        {
            Assert.NotEqual(Errors.General.NotFound(), Errors.General.AnErrorHasOccurred());
        }

        [Fact]
        public void Errors_with_the_same_code_hash_alike()
        {
            var seen = new HashSet<Error> { Errors.General.NotFound("1"), Errors.General.NotFound("2") };

            Assert.Single(seen);
        }

        [Fact]
        public async Task A_new_row_records_who_created_it()
        {
            await using var context = CreateContext(
                nameof(A_new_row_records_who_created_it),
                new TestUser(Guid.NewGuid(), "kaveh.ahangar", IsAuthenticated: true));

            var group = new CustomerGroup("VIP", "Key accounts");
            context.CustomerGroups.Add(group);
            await context.SaveChanges(Token);

            Assert.Equal("kaveh.ahangar", group.CreatedBy);
        }

        [Fact]
        public async Task A_changed_row_records_who_changed_it()
        {
            await using var context = CreateContext(
                nameof(A_changed_row_records_who_changed_it),
                new TestUser(Guid.NewGuid(), "kaveh.ahangar", IsAuthenticated: true));

            var group = new CustomerGroup("VIP", "Key accounts");
            context.CustomerGroups.Add(group);
            await context.SaveChanges(Token);

            group.Update("Key accounts", "revised");
            await context.SaveChanges(Token);

            Assert.Equal("kaveh.ahangar", group.ModifiedBy);
            Assert.NotNull(group.ModificationDateTime);
        }

        /// <summary>
        /// Creation is recorded once. Someone else changing the row later must not be able to
        /// rewrite who made it.
        /// </summary>
        [Fact]
        public async Task The_creator_is_never_overwritten()
        {
            var database = nameof(The_creator_is_never_overwritten);
            Guid groupId;

            await using (var first = CreateContext(database, new TestUser(Guid.NewGuid(), "kaveh.ahangar", true)))
            {
                var group = new CustomerGroup("VIP", "Key accounts");
                first.CustomerGroups.Add(group);
                await first.SaveChanges(Token);
                groupId = group.Id;
            }

            await using (var second = CreateContext(database, new TestUser(Guid.NewGuid(), "sara.esfandiari", true)))
            {
                var group = await second.CustomerGroups.SingleAsync(g => g.Id == groupId, Token);
                group.Update("Key accounts", "revised");
                await second.SaveChanges(Token);

                Assert.Equal("kaveh.ahangar", group.CreatedBy);
                Assert.Equal("sara.esfandiari", group.ModifiedBy);
            }
        }

        /// <summary>
        /// A soft delete already recorded who did it. Stamping it again as an ordinary change would
        /// overwrite that and rotate the concurrency stamp a second time for nothing.
        /// </summary>
        [Fact]
        public async Task A_soft_delete_keeps_the_name_of_whoever_deleted_it()
        {
            await using var context = CreateContext(
                nameof(A_soft_delete_keeps_the_name_of_whoever_deleted_it),
                new TestUser(Guid.NewGuid(), "kaveh.ahangar", true));

            var group = new CustomerGroup("VIP", "Key accounts");
            context.CustomerGroups.Add(group);
            await context.SaveChanges(Token);

            group.Delete("sara.esfandiari");
            await context.SaveChanges(Token);

            Assert.True(group.IsDeleted);
            Assert.Equal("sara.esfandiari", group.DeletedBy);
        }

        /// <summary>
        /// Work with nobody behind it writes no name rather than an invented one.
        /// </summary>
        [Fact]
        public async Task Work_with_no_user_leaves_the_name_empty()
        {
            await using var context = CreateContext(
                nameof(Work_with_no_user_leaves_the_name_empty),
                new TestUser(null, null, IsAuthenticated: false));

            var group = new CustomerGroup("VIP", "Key accounts");
            context.CustomerGroups.Add(group);
            await context.SaveChanges(Token);

            Assert.Null(group.CreatedBy);
        }
    }
}
