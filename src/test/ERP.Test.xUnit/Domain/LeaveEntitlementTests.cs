using ERP.Domain.Entities.Modules.HumanResourcesManagement;
using ERP.Domain.Entities.Users;

namespace ERP.Test.xUnit.Domain
{
    /// <summary>
    /// Leave has to be held before it is approved, otherwise the same days can be promised twice.
    /// </summary>
    public sealed class LeaveEntitlementTests
    {
        private static Human CreateEmployee()
        {
            var user = new User("employee", "hash:hash");
            return new Human(user, "Sara", "Ahmadi", HumanGender.Female, new DateOnly(1995, 5, 1), maritalStatus: false);
        }

        private static LeaveEntitlement CreateEntitlement(decimal days = 26m) =>
            new(CreateEmployee(), HumanRequestLeaveType.Paid, 1404, days);

        [Fact]
        public void A_new_entitlement_has_its_full_balance_available()
        {
            var entitlement = CreateEntitlement();

            Assert.Equal(26m, entitlement.RemainingDays);
        }

        [Fact]
        public void Reserving_days_holds_them_against_the_balance()
        {
            var entitlement = CreateEntitlement();

            entitlement.Reserve(5m);

            Assert.Equal(5m, entitlement.PendingDays);
            Assert.Equal(21m, entitlement.RemainingDays);
            Assert.Equal(0m, entitlement.UsedDays);
        }

        [Fact]
        public void The_same_days_cannot_be_reserved_twice()
        {
            var entitlement = CreateEntitlement(days: 5m);

            entitlement.Reserve(5m);

            Assert.Throws<InvalidOperationException>(() => entitlement.Reserve(1m));
        }

        [Fact]
        public void Approving_a_request_turns_held_days_into_used_days()
        {
            var entitlement = CreateEntitlement();

            entitlement.Reserve(3m);
            entitlement.Consume(3m);

            Assert.Equal(0m, entitlement.PendingDays);
            Assert.Equal(3m, entitlement.UsedDays);
            Assert.Equal(23m, entitlement.RemainingDays);
        }

        [Fact]
        public void Rejecting_a_request_gives_the_held_days_back()
        {
            var entitlement = CreateEntitlement();

            entitlement.Reserve(4m);
            entitlement.Release(4m);

            Assert.Equal(26m, entitlement.RemainingDays);
        }

        [Fact]
        public void Carried_over_days_add_to_the_balance()
        {
            var entitlement = CreateEntitlement();

            entitlement.CarryOver(4m);

            Assert.Equal(30m, entitlement.TotalDays);
            Assert.Equal(30m, entitlement.RemainingDays);
        }
    }
}
