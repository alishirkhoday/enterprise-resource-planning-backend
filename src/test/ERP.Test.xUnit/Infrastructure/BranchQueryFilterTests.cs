using ERP.Application.Common.Interfaces.Branches;
using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;
using ERP.Infrastructure.MainDatabase.Context;
using Microsoft.EntityFrameworkCore;

namespace ERP.Test.xUnit.Infrastructure
{
    /// <summary>
    /// The branch filter is the one piece of this design that cannot be argued into correctness.
    /// EF caches the model once per context type, so the danger is that the first caller's access
    /// level is baked into it and served to everyone afterwards. These tests settle it by asking
    /// two contexts with different access the same question against one database.
    /// </summary>
    public sealed class BranchQueryFilterTests
    {
        private sealed record TestBranchContext(
            Guid? CurrentBranchId,
            IReadOnlyCollection<Guid> AccessibleBranchIds,
            bool CanAccessAllBranches) : IBranchContext;

        private static readonly Guid Tehran = Guid.NewGuid();
        private static readonly Guid Shiraz = Guid.NewGuid();

        private static IBranchContext SeeingOnly(params Guid[] branchIds) =>
            new TestBranchContext(branchIds.FirstOrDefault(), branchIds, CanAccessAllBranches: false);

        private static IBranchContext SeeingEverything() =>
            new TestBranchContext(null, [], CanAccessAllBranches: true);

        private static DbContextOptions<MainDbContext> SharedDatabase(string name) =>
            new DbContextOptionsBuilder<MainDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

        /// <summary>
        /// Seeds one customer per branch plus one belonging to the organization as a whole. The
        /// seeding context can see everything, so what it writes is never shaped by the filter.
        /// </summary>
        private static async Task SeedAsync(DbContextOptions<MainDbContext> options)
        {
            await using var context = new MainDbContext(options, SeeingEverything());

            var tehran = new Customer("C-TEH", "Tehran Customer", CustomerType.Organization);
            tehran.AssignToBranch(Tehran);

            var shiraz = new Customer("C-SHI", "Shiraz Customer", CustomerType.Organization);
            shiraz.AssignToBranch(Shiraz);

            var organizationWide = new Customer("C-ALL", "Head Office Customer", CustomerType.Organization);

            context.Customers.AddRange(tehran, shiraz, organizationWide);
            await context.SaveChanges(TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task A_caller_sees_their_own_branch_and_the_organization_wide_records()
        {
            var options = SharedDatabase(nameof(A_caller_sees_their_own_branch_and_the_organization_wide_records));
            await SeedAsync(options);

            await using var context = new MainDbContext(options, SeeingOnly(Tehran));
            var codes = await context.Customers.Select(c => c.Code).ToListAsync(TestContext.Current.CancellationToken);

            Assert.Equal(["C-ALL", "C-TEH"], codes.Order());
        }

        /// <summary>
        /// The test that matters. Two contexts over the same database, created one after the other,
        /// must each answer for themselves - if the model had baked the first one's access in, the
        /// second would see Tehran's customer too.
        /// </summary>
        [Fact]
        public async Task Two_callers_against_one_database_each_see_their_own_branch()
        {
            var options = SharedDatabase(nameof(Two_callers_against_one_database_each_see_their_own_branch));
            await SeedAsync(options);

            List<string> tehranCodes;
            await using (var tehranContext = new MainDbContext(options, SeeingOnly(Tehran)))
            {
                tehranCodes = await tehranContext.Customers.Select(c => c.Code).ToListAsync(TestContext.Current.CancellationToken);
            }

            List<string> shirazCodes;
            await using (var shirazContext = new MainDbContext(options, SeeingOnly(Shiraz)))
            {
                shirazCodes = await shirazContext.Customers.Select(c => c.Code).ToListAsync(TestContext.Current.CancellationToken);
            }

            Assert.Contains("C-TEH", tehranCodes);
            Assert.DoesNotContain("C-SHI", tehranCodes);

            Assert.Contains("C-SHI", shirazCodes);
            Assert.DoesNotContain("C-TEH", shirazCodes);
        }

        [Fact]
        public async Task A_regional_caller_sees_every_branch_they_cover()
        {
            var options = SharedDatabase(nameof(A_regional_caller_sees_every_branch_they_cover));
            await SeedAsync(options);

            await using var context = new MainDbContext(options, SeeingOnly(Tehran, Shiraz));
            var codes = await context.Customers.Select(c => c.Code).ToListAsync(TestContext.Current.CancellationToken);

            Assert.Equal(["C-ALL", "C-SHI", "C-TEH"], codes.Order());
        }

        [Fact]
        public async Task Head_office_sees_everything()
        {
            var options = SharedDatabase(nameof(Head_office_sees_everything));
            await SeedAsync(options);

            await using var context = new MainDbContext(options, SeeingEverything());
            var codes = await context.Customers.Select(c => c.Code).ToListAsync(TestContext.Current.CancellationToken);

            Assert.Equal(3, codes.Count);
        }

        [Fact]
        public async Task A_caller_with_no_branches_sees_only_organization_wide_records()
        {
            var options = SharedDatabase(nameof(A_caller_with_no_branches_sees_only_organization_wide_records));
            await SeedAsync(options);

            await using var context = new MainDbContext(options, SeeingOnly());
            var codes = await context.Customers.Select(c => c.Code).ToListAsync(TestContext.Current.CancellationToken);

            Assert.Equal(["C-ALL"], codes);
        }

        /// <summary>
        /// The reason the filter is set-based rather than single-branch: a cross-branch view must
        /// never need IgnoreQueryFilters, because that switches off soft delete at the same time
        /// and brings deleted rows back.
        /// </summary>
        [Fact]
        public async Task Seeing_another_branch_does_not_resurrect_deleted_rows()
        {
            var options = SharedDatabase(nameof(Seeing_another_branch_does_not_resurrect_deleted_rows));
            await SeedAsync(options);

            await using (var seeding = new MainDbContext(options, SeeingEverything()))
            {
                var shiraz = await seeding.Customers.SingleAsync(c => c.Code == "C-SHI", TestContext.Current.CancellationToken);
                shiraz.Delete("tester");
                await seeding.SaveChanges(TestContext.Current.CancellationToken);
            }

            await using var regional = new MainDbContext(options, SeeingOnly(Tehran, Shiraz));
            var codes = await regional.Customers.Select(c => c.Code).ToListAsync(TestContext.Current.CancellationToken);

            Assert.DoesNotContain("C-SHI", codes);
            Assert.Contains("C-TEH", codes);
        }

        /// <summary>
        /// An entity with no branch of its own is untouched by the filter, so reference data stays
        /// visible from everywhere.
        /// </summary>
        [Fact]
        public async Task An_entity_that_is_not_branch_scoped_is_unaffected()
        {
            var options = SharedDatabase(nameof(An_entity_that_is_not_branch_scoped_is_unaffected));

            await using (var seeding = new MainDbContext(options, SeeingEverything()))
            {
                seeding.CustomerGroups.Add(new CustomerGroup("VIP", "Key accounts"));
                await seeding.SaveChanges(TestContext.Current.CancellationToken);
            }

            await using var context = new MainDbContext(options, SeeingOnly(Tehran));
            var groups = await context.CustomerGroups.ToListAsync(TestContext.Current.CancellationToken);

            Assert.Single(groups);
        }
    }
}
