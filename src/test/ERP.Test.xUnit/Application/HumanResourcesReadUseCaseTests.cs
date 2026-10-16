using CSharpFunctionalExtensions;
using ERP.Application.Common;
using ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.GrantLeaveEntitlement;
using ERP.Application.UseCases.Modules.HumanResourcesManagement.DTOs;
using ERP.Application.UseCases.Modules.HumanResourcesManagement.Queries.GetAttendanceSummary;
using ERP.Application.UseCases.Modules.HumanResourcesManagement.Queries.GetExpiringContracts;
using ERP.Domain.Entities.Common;
using ERP.Domain.Entities.Modules.HumanResourcesManagement;
using ERP.Domain.Entities.Users;
using ERP.Domain.Repositories.Modules.HumanResourcesManagement;
using Moq;

namespace ERP.Test.xUnit.Application
{
    /// <summary>
    /// The three pieces the human resources module was resting on and did not have: a leave
    /// balance somebody can actually be granted, the attendance figure payroll pays against, and
    /// a warning before a contract lapses under somebody who is still turning up.
    /// </summary>
    public sealed class HumanResourcesReadUseCaseTests
    {
        private static readonly DateTimeOffset Now = new(2026, 5, 10, 9, 0, 0, TimeSpan.Zero);
        private static readonly DateOnly Today = new(2026, 5, 10);

        private static CancellationToken Token => TestContext.Current.CancellationToken;

        private sealed class FixedClock(DateTimeOffset now) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => now;
        }

        private static Human CreateHuman(HumanStatus status = HumanStatus.Active)
        {
            var human = new Human(
                new User("kaveh.ahangar", "hash"),
                "Kaveh",
                "Ahangar",
                HumanGender.Male,
                new DateOnly(1990, 5, 12),
                maritalStatus: false);

            human.Hire(new DateOnly(2020, 1, 15));

            if (status == HumanStatus.Terminated)
                human.Terminate(new DateOnly(2024, 6, 30));

            return human;
        }

        private static GrantLeaveEntitlementCommand GrantCommand(Guid humanId, decimal days = 26m, decimal carriedOver = 0m) => new()
        {
            HumanId = humanId,
            LeaveType = HumanRequestLeaveType.Paid,
            Year = 2026,
            EntitledDays = days,
            CarriedOverDays = carriedOver
        };

        private static (Mock<IHumanRepository> Humans, Mock<ILeaveRepository> Leave) Repositories(Human? human, LeaveEntitlement? existing = null)
        {
            var humans = new Mock<IHumanRepository>();
            humans.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(human);

            var leave = new Mock<ILeaveRepository>();
            leave.Setup(r => r.GetEntitlementAsync(
                    It.IsAny<Guid>(), It.IsAny<HumanRequestLeaveType>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);

            return (humans, leave);
        }

        [Fact]
        public async Task A_balance_can_be_opened_for_somebody_who_had_none()
        {
            var human = CreateHuman();
            var (humans, leave) = Repositories(human);

            var result = await new GrantLeaveEntitlementCommandHandler(humans.Object, leave.Object)
                .Handle(GrantCommand(human.Id), Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(26m, result.Value.EntitledDays);
            Assert.Equal(26m, result.Value.RemainingDays);

            leave.Verify(r => r.AddEntitlementAsync(It.IsAny<LeaveEntitlement>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Restating_a_balance_leaves_days_already_taken_alone()
        {
            // Wiping them would hand back leave somebody has already used.
            var human = CreateHuman();
            var existing = new LeaveEntitlement(human, HumanRequestLeaveType.Paid, 2026, 20m);
            existing.Reserve(5m);
            existing.Consume(5m);

            var (humans, leave) = Repositories(human, existing);

            var result = await new GrantLeaveEntitlementCommandHandler(humans.Object, leave.Object)
                .Handle(GrantCommand(human.Id, days: 26m), Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(26m, result.Value.EntitledDays);
            Assert.Equal(5m, result.Value.UsedDays);
            Assert.Equal(21m, result.Value.RemainingDays);

            leave.Verify(r => r.AddEntitlementAsync(It.IsAny<LeaveEntitlement>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Carried_over_days_add_to_what_is_available()
        {
            var human = CreateHuman();
            var (humans, leave) = Repositories(human);

            var result = await new GrantLeaveEntitlementCommandHandler(humans.Object, leave.Object)
                .Handle(GrantCommand(human.Id, days: 26m, carriedOver: 4m), Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(4m, result.Value.CarriedOverDays);
            Assert.Equal(30m, result.Value.RemainingDays);
        }

        [Fact]
        public async Task A_terminated_employee_gets_no_new_balance()
        {
            var human = CreateHuman(HumanStatus.Terminated);
            var (humans, leave) = Repositories(human);

            var result = await new GrantLeaveEntitlementCommandHandler(humans.Object, leave.Object)
                .Handle(GrantCommand(human.Id), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("EmployeeIsTerminated", result.Error.Code);
        }

        private static HumanAttendanceRecord CreateRecord(Human human, DateOnly date, TimeOnly? entry, TimeOnly? exit)
        {
            var record = new HumanAttendanceRecord(human, date);

            if (entry is not null)
                record.SetEntryTime(entry.Value);

            if (exit is not null)
                record.SetExitTime(exit.Value);

            return record;
        }

        private static async Task<Result<AttendanceSummaryDto, Error>> SummariseAsync(
            Human human,
            params HumanAttendanceRecord[] records)
        {
            var humans = new Mock<IHumanRepository>();
            humans.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(human);

            var attendance = new Mock<IAttendanceRepository>();
            attendance.Setup(r => r.GetBetweenAsync(
                    It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(records);

            return await new GetAttendanceSummaryQueryHandler(humans.Object, attendance.Object).Handle(
                new GetAttendanceSummaryQuery { HumanId = human.Id, From = Today.AddDays(-7), To = Today },
                Token);
        }

        [Fact]
        public async Task Worked_hours_are_summed_across_complete_days()
        {
            var human = CreateHuman();

            var result = await SummariseAsync(
                human,
                CreateRecord(human, Today.AddDays(-2), new TimeOnly(8, 0), new TimeOnly(16, 0)),
                CreateRecord(human, Today.AddDays(-1), new TimeOnly(8, 30), new TimeOnly(17, 0)));

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.CompleteDays);
            Assert.Equal(16.5m, result.Value.TotalWorkedHours);
        }

        [Fact]
        public async Task A_day_with_no_exit_reports_no_hours_and_is_counted_separately()
        {
            // Zero would read as somebody who came in and did nothing. Null says the clock never
            // saw them leave, which is a different problem with a different fix.
            var human = CreateHuman();

            var result = await SummariseAsync(
                human,
                CreateRecord(human, Today.AddDays(-2), new TimeOnly(8, 0), new TimeOnly(16, 0)),
                CreateRecord(human, Today.AddDays(-1), new TimeOnly(8, 0), null));

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.RecordedDays);
            Assert.Equal(1, result.Value.CompleteDays);
            Assert.Equal(1, result.Value.IncompleteDays);
            Assert.Equal(8m, result.Value.TotalWorkedHours);
            Assert.Null(result.Value.Days.Single(d => !d.IsComplete).WorkedHours);
        }

        [Fact]
        public async Task A_period_with_no_records_reports_nothing_rather_than_failing()
        {
            var human = CreateHuman();

            var result = await SummariseAsync(human);

            Assert.True(result.IsSuccess);
            Assert.Equal(0, result.Value.RecordedDays);
            Assert.Equal(0m, result.Value.TotalWorkedHours);
        }

        private static HumanContract CreateContract(Human human, DateOnly endDate)
        {
            var position = new Position(new Department("D-1", "Engineering"), "Engineer");

            return new HumanContract(human, position, HumanEmploymentType.FullTime,
                DateRange.Create(new DateOnly(2025, 1, 1), endDate));
        }

        [Fact]
        public async Task Days_remaining_are_measured_from_today_not_from_the_window()
        {
            // A list asked for next month still has to say how long is actually left, or the
            // number means something different depending on when it was requested.
            var human = CreateHuman();
            var contract = CreateContract(human, Today.AddDays(20));

            var contracts = new Mock<IEmploymentContractRepository>();
            contracts.Setup(r => r.GetExpiringBetweenAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([contract]);

            var result = await new GetExpiringContractsQueryHandler(contracts.Object, new FixedClock(Now))
                .Handle(new GetExpiringContractsQuery { WithinDays = 30 }, Token);

            Assert.True(result.IsSuccess);

            var expiring = Assert.Single(result.Value);
            Assert.Equal(20, expiring.DaysRemaining);
            Assert.Equal(human.FullName, expiring.EmployeeName);
        }

        [Fact]
        public async Task Nothing_expiring_is_an_empty_list_rather_than_a_failure()
        {
            var contracts = new Mock<IEmploymentContractRepository>();
            contracts.Setup(r => r.GetExpiringBetweenAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            var result = await new GetExpiringContractsQueryHandler(contracts.Object, new FixedClock(Now))
                .Handle(new GetExpiringContractsQuery(), Token);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value);
        }
    }
}
