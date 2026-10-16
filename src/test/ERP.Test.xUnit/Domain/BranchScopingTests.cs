using System.Reflection;
using ERP.Domain.Entities.Branches;
using ERP.Domain.Entities.Security;

namespace ERP.Test.xUnit.Domain
{
    /// <summary>
    /// Branch isolation is only as good as its weakest entity: one record carrying a BranchId that
    /// does not declare <see cref="IBranchScoped"/> is one the query filter will never reach, and
    /// the leak is silent. These tests make that a build failure instead.
    /// </summary>
    public sealed class BranchScopingTests
    {
        private static readonly Assembly DomainAssembly = typeof(BaseEntity).Assembly;

        /// <summary>
        /// Rows that decide who may see which branch cannot themselves be filtered by branch: the
        /// filter would need them to know what it is allowed to show.
        /// </summary>
        private static readonly Type[] DefinesBranchAccess =
        [
            typeof(UserRole),
            typeof(BranchUser),
            typeof(BranchUserRole),
            typeof(BranchCustomer),
            typeof(BranchHuman),
            typeof(BranchOrder)
        ];

        private static IEnumerable<Type> Entities =>
            DomainAssembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false } && t.IsAssignableTo(typeof(BaseEntity)));

        public static TheoryData<Type> EntitiesCarryingABranch()
        {
            var data = new TheoryData<Type>();
            foreach (var type in Entities
                         .Where(t => t.GetProperty("BranchId") is not null && !DefinesBranchAccess.Contains(t))
                         .OrderBy(t => t.Name, StringComparer.Ordinal))
            {
                data.Add(type);
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(EntitiesCarryingABranch))]
        public void An_entity_that_carries_a_branch_declares_that_it_is_scoped_to_one(Type entityType)
        {
            Assert.True(
                entityType.IsAssignableTo(typeof(IBranchScoped)),
                $"{entityType.Name} has a BranchId but does not implement IBranchScoped, so no branch filter can reach it.");
        }

        /// <summary>
        /// A null branch means the record belongs to the organization as a whole and stays visible
        /// everywhere. A non-nullable identifier could not express that, and the interface would
        /// not fit it.
        /// </summary>
        [Theory]
        [MemberData(nameof(EntitiesCarryingABranch))]
        public void The_branch_is_optional_so_organization_wide_records_can_exist(Type entityType)
        {
            var property = entityType.GetProperty("BranchId");

            Assert.NotNull(property);
            Assert.Equal(typeof(Guid?), property!.PropertyType);
        }

        [Fact]
        public void The_rows_that_define_branch_access_are_not_themselves_scoped()
        {
            var offenders = DefinesBranchAccess
                .Where(t => t.IsAssignableTo(typeof(IBranchScoped)))
                .Select(t => t.Name)
                .ToArray();

            Assert.True(
                offenders.Length == 0,
                $"Filtering these by branch would be circular: {string.Join(", ", offenders)}.");
        }

        [Fact]
        public void Every_scoped_entity_can_be_assigned_to_a_branch()
        {
            var offenders = Entities
                .Where(t => t.IsAssignableTo(typeof(IBranchScoped)))
                .Where(t => t.GetMethod(nameof(IBranchScoped.AssignToBranch), [typeof(Guid)]) is null)
                .Select(t => t.Name)
                .ToArray();

            Assert.True(offenders.Length == 0, $"No AssignToBranch on: {string.Join(", ", offenders)}.");
        }
    }
}
