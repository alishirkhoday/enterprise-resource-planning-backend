using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;
using ERP.Domain.Entities.Modules.FinanceManagement;
using ERP.Domain.Entities.Modules.HumanResourcesManagement;
using ERP.Domain.Entities.Modules.OrderManagement;
using ERP.Domain.Entities.Modules.ProductionManagement;
using ERP.Domain.Entities.Modules.ProjectManagement;
using ERP.Domain.Entities.Security;
using ERP.Domain.Entities.Users;
using ERP.Domain.Entities.Workflows;

namespace ERP.Test.xUnit.Domain
{
    /// <summary>
    /// A domain event states that something happened. These tests hold it to that: it is raised
    /// when the thing happens, it is not raised when it does not, and repeating a call that changes
    /// nothing does not announce the fact a second time.
    /// </summary>
    public sealed class DomainEventTests
    {
        private static User CreateUser() => new("kaveh.ahangar", "hash-of-the-first-password");

        private static Human CreateHuman() =>
            new(CreateUser(), "Kaveh", "Ahangar", HumanGender.Male, new DateOnly(1990, 5, 12), maritalStatus: false);

        [Fact]
        public void Registering_a_user_does_not_announce_a_password_change()
        {
            var user = CreateUser();

            Assert.Contains(user.DomainEvents, e => e is UserRegistered);
            Assert.DoesNotContain(user.DomainEvents, e => e is UserPasswordChanged);
        }

        [Fact]
        public void Replacing_a_password_announces_the_change()
        {
            var user = CreateUser();
            user.ClearDomainEvents();

            user.SetPassword("hash-of-the-second-password");

            Assert.Contains(user.DomainEvents, e => e is UserPasswordChanged);
        }

        [Fact]
        public void Signing_in_carries_where_it_came_from()
        {
            var user = CreateUser();
            user.ClearDomainEvents();

            user.RecordSignIn("192.0.2.10", "Chrome on Windows");

            var signedIn = Assert.Single(user.DomainEvents.OfType<UserSignedIn>());
            Assert.Equal("192.0.2.10", signedIn.InternetProtocolAddress);
            Assert.Equal("Chrome on Windows", signedIn.DeviceName);
        }

        [Fact]
        public void Assigning_a_role_twice_announces_it_once()
        {
            var user = CreateUser();
            var role = new Role("Accountant");
            user.ClearDomainEvents();

            user.AssignRole(role);
            user.AssignRole(role);

            Assert.Single(user.DomainEvents.OfType<UserRoleAssigned>());
        }

        [Fact]
        public void Revoking_a_role_that_was_never_held_announces_nothing()
        {
            var user = CreateUser();
            var role = new Role("Accountant");
            user.ClearDomainEvents();

            user.RevokeRole(role);

            Assert.Empty(user.DomainEvents.OfType<UserRoleRevoked>());
        }

        [Fact]
        public void Approving_a_leave_request_announces_the_leave()
        {
            var human = CreateHuman();
            var request = new HumanRequest(
                human,
                HumanRequestType.LeaveDaily,
                new DateOnly(2026, 4, 1),
                new DateOnly(2026, 4, 3),
                leaveType: HumanRequestLeaveType.Paid);

            request.SetApprove();

            var approved = Assert.Single(request.DomainEvents.OfType<LeaveRequestApproved>());
            Assert.Equal(new DateOnly(2026, 4, 1), approved.StartDate);
            Assert.Equal(new DateOnly(2026, 4, 3), approved.EndDate);
        }

        /// <summary>
        /// Remote work travels through the same request type as leave. Payroll must not treat it as
        /// time off, so the event does not fire for it.
        /// </summary>
        [Fact]
        public void Approving_remote_work_is_not_a_leave()
        {
            var human = CreateHuman();
            var request = new HumanRequest(
                human,
                HumanRequestType.RemoteWork,
                new DateOnly(2026, 4, 1),
                new DateOnly(2026, 4, 3));

            request.SetApprove();

            Assert.Empty(request.DomainEvents.OfType<LeaveRequestApproved>());
        }

        [Fact]
        public void A_request_cannot_be_decided_twice()
        {
            var human = CreateHuman();
            var request = new HumanRequest(
                human,
                HumanRequestType.LeaveDaily,
                new DateOnly(2026, 4, 1),
                new DateOnly(2026, 4, 3),
                leaveType: HumanRequestLeaveType.Sick);

            request.SetApprove();

            Assert.Throws<InvalidOperationException>(() => request.SetApprove());
            Assert.Single(request.DomainEvents.OfType<LeaveRequestApproved>());
        }

        [Fact]
        public void Terminating_an_employee_twice_announces_it_once()
        {
            var human = CreateHuman();
            human.Hire(new DateOnly(2020, 1, 15));

            human.Terminate(new DateOnly(2026, 2, 20));
            human.Terminate(new DateOnly(2026, 2, 20));

            Assert.Single(human.DomainEvents.OfType<EmployeeTerminated>());
        }

        [Fact]
        public void Raising_an_approval_request_announces_the_step_it_waits_on()
        {
            var workflow = new ApprovalWorkflow("purchase-order", "Purchase order approval", "PurchaseOrder");
            workflow.AddStep(new ApprovalWorkflowStep(workflow, 1, "Department head", ApproverType.DirectManager));

            var request = new ApprovalRequest(workflow, "PurchaseOrder", Guid.NewGuid().ToString(), Guid.NewGuid());

            var requested = Assert.Single(request.DomainEvents.OfType<ApprovalRequested>());
            Assert.Equal(1, requested.StepOrder);
            Assert.Equal("PurchaseOrder", requested.ReferenceType);
        }

        [Fact]
        public void Reversing_a_posted_entry_announces_both_documents()
        {
            var year = new FinancialYear("1404", new DateOnly(2025, 3, 21), new DateOnly(2026, 3, 20));
            year.AddPeriod(new AccountingPeriod(year, 1, "Farvardin", DateRange.Create(new DateOnly(2025, 3, 21), new DateOnly(2025, 4, 20))));
            year.Open(Guid.NewGuid());

            var entry = new Document(year, "Cash sale", new DateTimeOffset(new DateTime(2025, 4, 1), TimeSpan.Zero), "Test entry", DocumentType.Receive);
            var cash = new Account("1001", "Cash", AccountType.AssetCurrent, AccountGroup.LevelFour, AccountBalance.Debtor, FinancialStatement.BalanceSheet, isFinal: true);
            var revenue = new Account("4001", "Revenue", AccountType.RevenueOperating, AccountGroup.LevelFour, AccountBalance.Creditor, FinancialStatement.IncomeAndExpenseStatement, isFinal: true);
            entry.AddItem(new DocumentItem(entry, cash, 1, 1_000m, 0m));
            entry.AddItem(new DocumentItem(entry, revenue, 2, 0m, 1_000m));
            entry.Post();
            entry.ClearDomainEvents();

            var reversingDocumentId = Guid.NewGuid();
            entry.MarkAsReversed(reversingDocumentId);

            var reversed = Assert.Single(entry.DomainEvents.OfType<JournalEntryReversed>());
            Assert.Equal(entry.Id, reversed.OriginalDocumentId);
            Assert.Equal(reversingDocumentId, reversed.ReversingDocumentId);
        }

        [Fact]
        public void Achieving_a_milestone_announces_the_date_it_was_reached()
        {
            var project = new Project(
                "PRJ-1",
                "Warehouse rollout",
                ProjectType.Customer,
                DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)),
                "IRR");
            var phase = new ProjectPhase(project, "Design", 1, DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31)), Percentage.Create(25m));
            var milestone = new ProjectMilestone(phase, "Design signed off", new DateOnly(2026, 3, 20));

            milestone.Achieve(new DateOnly(2026, 3, 18));

            var achieved = Assert.Single(milestone.DomainEvents.OfType<ProjectMilestoneAchieved>());
            Assert.Equal(project.Id, achieved.ProjectId);
            Assert.Equal(new DateOnly(2026, 3, 18), achieved.AchievedOn);
        }

        [Fact]
        public void A_clean_quality_check_announces_nothing()
        {
            var check = new QualityControlCheck(CreateHuman(), inspectedQuantity: 100m);

            check.RecordResult(passedQuantity: 100m);

            Assert.True(check.Passed);
            Assert.Empty(check.DomainEvents.OfType<QualityCheckFailed>());
        }

        [Fact]
        public void A_quality_check_with_rejects_announces_how_many_failed()
        {
            var check = new QualityControlCheck(CreateHuman(), inspectedQuantity: 100m);

            check.RecordResult(passedQuantity: 93m, notes: "Surface finish out of spec.");

            var failed = Assert.Single(check.DomainEvents.OfType<QualityCheckFailed>());
            Assert.Equal(7m, failed.FailedQuantity);
        }

        [Fact]
        public void Cancelling_an_order_twice_announces_it_once()
        {
            var order = new Order(new Customer("C-1", "Sepehr Trading", CustomerType.Organization), "ORD-1", "IRR");

            order.Cancel("Customer changed their mind.");
            order.Cancel("Customer changed their mind.");

            Assert.Single(order.DomainEvents.OfType<OrderCancelled>());
            Assert.Equal(OrderStatus.Cancelled, order.Status);
        }

        [Fact]
        public void An_empty_order_cannot_be_placed()
        {
            var order = new Order(new Customer("C-1", "Sepehr Trading", CustomerType.Organization), "ORD-1", "IRR");

            Assert.Throws<InvalidOperationException>(() => order.Place());
            Assert.Empty(order.DomainEvents.OfType<OrderPlaced>());
        }
    }
}
