using ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.DecideLeaveRequest;
using ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.IssueEmploymentContract;
using ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.RecordAttendance;
using ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.RequestLeave;
using ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.RunPayroll;
using ERP.Domain.Entities.Modules.HumanResourcesManagement;
using ERP.Domain.Entities.Users;
using ERP.Domain.Repositories.Modules.FinanceManagement;
using ERP.Domain.Repositories.Modules.HumanResourcesManagement;
using Moq;

namespace ERP.Test.xUnit.Application
{
    /// <summary>
    /// The rules that decide somebody's pay and their days off. Each of these guards a mistake
    /// that is only noticed once money or time has already been given away.
    /// </summary>
    public sealed class HumanResourcesUseCaseTests
    {
        private static CancellationToken Token => TestContext.Current.CancellationToken;

        private static Human CreateHuman(HumanStatus status = HumanStatus.Active)
        {
            var human = new Human(
                new User("kaveh.ahangar", "hash"),
                "Kaveh",
                "Ahangar",
                HumanGender.Male,
                new DateOnly(1990, 5, 12),
                maritalStatus: false);

            if (status == HumanStatus.Active)
                human.Hire(new DateOnly(2020, 1, 15));
            else if (status == HumanStatus.Terminated)
            {
                human.Hire(new DateOnly(2020, 1, 15));
                human.Terminate(new DateOnly(2024, 6, 30));
            }

            return human;
        }

        private static Position CreatePosition() => new(new Department("D-1", "Engineering"), "Engineer");

        /// <summary>
        /// The four repositories human resources was split into, wired to one person.
        /// <para>
        /// They used to be a single contract, which meant every handler here took the same
        /// dependency and a constructor said nothing about what it touched. Keeping them together
        /// in the harness is convenience for the tests; keeping them apart in the handlers is what
        /// makes a clock-in stop reaching for contracts and leave balances.
        /// </para>
        /// </summary>
        private sealed class HrMocks
        {
            public Mock<IHumanRepository> Humans { get; } = new();
            public Mock<IEmploymentContractRepository> Contracts { get; } = new();
            public Mock<IAttendanceRepository> Attendance { get; } = new();
            public Mock<ILeaveRepository> Leave { get; } = new();

            public HrMocks(Human? human)
            {
                Humans.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(human);
                Humans.Setup(r => r.GetWithContractsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(human);
            }

            public IssueEmploymentContractCommandHandler ContractHandler() => new(Humans.Object, Contracts.Object);

            public RecordAttendanceCommandHandler AttendanceHandler() => new(Humans.Object, Attendance.Object);

            public RequestLeaveCommandHandler LeaveHandler() => new(Humans.Object, Leave.Object);

            public DecideLeaveRequestCommandHandler DecisionHandler() => new(Leave.Object);

            public RunPayrollCommandHandler PayrollHandler(Mock<IPayrollRepository> payrolls) =>
                new(Humans.Object, Contracts.Object, payrolls.Object);
        }

        private static IssueEmploymentContractCommand ContractCommand(Guid humanId, Guid positionId) => new()
        {
            HumanId = humanId,
            PositionId = positionId,
            EmploymentType = HumanEmploymentType.FullTime,
            StartDate = new DateOnly(2026, 1, 1),
            BaseSalary = 100_000_000m,
            CurrencyCode = "IRR"
        };

        /// <summary>
        /// Payroll asks for the contract effective on a date and takes one answer. Two in force on
        /// the same day means whichever row comes back first decides a salary.
        /// </summary>
        [Fact]
        public async Task A_contract_overlapping_an_existing_one_is_refused()
        {
            var human = CreateHuman();
            var position = CreatePosition();
            var existing = new HumanContract(human, position, HumanEmploymentType.FullTime,
                DateRange.Create(new DateOnly(2025, 1, 1), new DateOnly(2026, 12, 31)));

            var hr = new HrMocks(human);
            hr.Contracts.Setup(r => r.GetEffectiveAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);

            var result = await hr.ContractHandler()
                .Handle(ContractCommand(human.Id, position.Id), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("ContractOverlaps", result.Error.Code);
        }

        [Fact]
        public async Task A_terminated_employee_cannot_be_given_a_contract()
        {
            var human = CreateHuman(HumanStatus.Terminated);
            var hr = new HrMocks(human);

            var result = await hr.ContractHandler()
                .Handle(ContractCommand(human.Id, Guid.NewGuid()), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("EmployeeIsTerminated", result.Error.Code);
        }

        [Fact]
        public async Task An_issued_contract_carries_its_salary()
        {
            var human = CreateHuman();
            var position = CreatePosition();

            var hr = new HrMocks(human);
            hr.Humans.Setup(r => r.GetPositionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(position);

            var result = await hr.ContractHandler()
                .Handle(ContractCommand(human.Id, position.Id), Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(100_000_000m, result.Value.BaseSalary);
            Assert.Equal("IRR", result.Value.CurrencyCode);
        }

        /// <summary>
        /// An exit usually arrives on its own, hours after the entry it has to follow, so the
        /// comparison is against what is stored rather than what came in with it.
        /// </summary>
        [Fact]
        public async Task An_exit_earlier_than_the_stored_entry_is_refused()
        {
            var human = CreateHuman();
            var date = new DateOnly(2026, 3, 2);
            var record = new HumanAttendanceRecord(human, date);
            record.SetEntryTime(new TimeOnly(8, 0));

            var hr = new HrMocks(human);
            hr.Attendance.Setup(r => r.GetForDayAsync(It.IsAny<Guid>(), date, It.IsAny<CancellationToken>())).ReturnsAsync(record);

            var result = await hr.AttendanceHandler().Handle(
                new RecordAttendanceCommand { HumanId = human.Id, Date = date, ExitTime = new TimeOnly(7, 0) },
                Token);

            Assert.True(result.IsFailure);
            Assert.Equal("ExitBeforeEntry", result.Error.Code);
        }

        /// <summary>
        /// A person who leaves for lunch and comes back has not begun a second working day.
        /// </summary>
        [Fact]
        public async Task A_second_call_completes_the_same_day_rather_than_starting_another()
        {
            var human = CreateHuman();
            var date = new DateOnly(2026, 3, 2);
            var record = new HumanAttendanceRecord(human, date);
            record.SetEntryTime(new TimeOnly(8, 0));

            var hr = new HrMocks(human);
            hr.Attendance.Setup(r => r.GetForDayAsync(It.IsAny<Guid>(), date, It.IsAny<CancellationToken>())).ReturnsAsync(record);

            var result = await hr.AttendanceHandler().Handle(
                new RecordAttendanceCommand { HumanId = human.Id, Date = date, ExitTime = new TimeOnly(17, 30) },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(record.Id, result.Value.Id);
            Assert.Equal(new TimeOnly(8, 0), result.Value.EntryTime);
            hr.Attendance.Verify(r => r.AddAsync(It.IsAny<HumanAttendanceRecord>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        private static RequestLeaveCommand LeaveCommand(Guid humanId, DateOnly from, DateOnly to) => new()
        {
            HumanId = humanId,
            Type = HumanRequestType.LeaveDaily,
            LeaveType = HumanRequestLeaveType.Paid,
            StartDate = from,
            EndDate = to
        };

        /// <summary>
        /// The reason days are held on asking rather than on approval: otherwise the same week can
        /// be booked repeatedly while the first request sits in a queue.
        /// </summary>
        [Fact]
        public async Task Asking_for_leave_holds_the_days_immediately()
        {
            var human = CreateHuman();
            var entitlement = new LeaveEntitlement(human, HumanRequestLeaveType.Paid, 2026, 26m);

            var hr = new HrMocks(human);
            hr.Leave.Setup(r => r.GetOverlappingRequestsAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            hr.Leave.Setup(r => r.GetEntitlementAsync(It.IsAny<Guid>(), It.IsAny<HumanRequestLeaveType>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(entitlement);

            var result = await hr.LeaveHandler()
                .Handle(LeaveCommand(human.Id, new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 3)), Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(3m, entitlement.PendingDays);
            Assert.Equal(23m, entitlement.RemainingDays);
        }

        [Fact]
        public async Task Leave_that_clashes_with_an_existing_request_is_refused()
        {
            var human = CreateHuman();
            var clash = new HumanRequest(human, HumanRequestType.LeaveDaily, new DateOnly(2026, 4, 2), new DateOnly(2026, 4, 5),
                leaveType: HumanRequestLeaveType.Paid);

            var hr = new HrMocks(human);
            hr.Leave.Setup(r => r.GetOverlappingRequestsAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([clash]);

            var result = await hr.LeaveHandler()
                .Handle(LeaveCommand(human.Id, new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 3)), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("LeaveOverlaps", result.Error.Code);
        }

        [Fact]
        public async Task Leave_beyond_the_entitlement_is_refused()
        {
            var human = CreateHuman();
            var entitlement = new LeaveEntitlement(human, HumanRequestLeaveType.Paid, 2026, 2m);

            var hr = new HrMocks(human);
            hr.Leave.Setup(r => r.GetOverlappingRequestsAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            hr.Leave.Setup(r => r.GetEntitlementAsync(It.IsAny<Guid>(), It.IsAny<HumanRequestLeaveType>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(entitlement);

            var result = await hr.LeaveHandler()
                .Handle(LeaveCommand(human.Id, new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 10)), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("NotEnoughLeave", result.Error.Code);
            Assert.Equal(0m, entitlement.PendingDays);
        }

        /// <summary>
        /// Held days have to go somewhere. Approval spends them, refusal returns them; leaving
        /// them held means days reserved against leave nobody will take.
        /// </summary>
        [Theory]
        [InlineData(true, 3.0, 0.0)]
        [InlineData(false, 0.0, 0.0)]
        public async Task Deciding_a_request_settles_the_days_that_were_held(bool isApproved, double expectedUsed, double expectedPending)
        {
            var human = CreateHuman();
            var entitlement = new LeaveEntitlement(human, HumanRequestLeaveType.Paid, 2026, 26m);
            entitlement.Reserve(3m);

            var request = new HumanRequest(human, HumanRequestType.LeaveDaily, new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 3),
                leaveType: HumanRequestLeaveType.Paid);

            var hr = new HrMocks(human);
            hr.Leave.Setup(r => r.GetRequestAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(request);
            hr.Leave.Setup(r => r.GetEntitlementAsync(It.IsAny<Guid>(), It.IsAny<HumanRequestLeaveType>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(entitlement);

            var result = await hr.DecisionHandler().Handle(
                new DecideLeaveRequestCommand { RequestId = request.Id, IsApproved = isApproved, Reason = "Not this week." },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal((decimal)expectedUsed, entitlement.UsedDays);
            Assert.Equal((decimal)expectedPending, entitlement.PendingDays);
        }

        [Fact]
        public async Task A_request_already_decided_cannot_be_decided_again()
        {
            var human = CreateHuman();
            var request = new HumanRequest(human, HumanRequestType.LeaveDaily, new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 3),
                leaveType: HumanRequestLeaveType.Paid);
            request.SetApprove();

            var hr = new HrMocks(human);
            hr.Leave.Setup(r => r.GetRequestAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(request);

            var result = await hr.DecisionHandler().Handle(
                new DecideLeaveRequestCommand { RequestId = request.Id, IsApproved = false, Reason = "Changed my mind." },
                Token);

            Assert.True(result.IsFailure);
            Assert.Equal("RequestIsAlreadyDecided", result.Error.Code);
        }

        private static RunPayrollCommand PayrollCommand(Guid humanId) => new()
        {
            HumanId = humanId,
            StartDate = new DateOnly(2026, 3, 1),
            EndDate = new DateOnly(2026, 3, 31),
            WorkedDays = 22m
        };

        /// <summary>
        /// The single most expensive mistake this module can make, so it is refused rather than
        /// merged into what is already there.
        /// </summary>
        [Fact]
        public async Task Running_the_same_period_twice_is_refused()
        {
            var human = CreateHuman();
            var hr = new HrMocks(human);

            var payrolls = new Mock<IPayrollRepository>();
            payrolls.Setup(r => r.ExistsForPeriodAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await hr.PayrollHandler(payrolls)
                .Handle(PayrollCommand(human.Id), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("PayrollAlreadyRun", result.Error.Code);
        }

        [Fact]
        public async Task Payroll_without_an_effective_contract_is_refused()
        {
            var human = CreateHuman();
            var hr = new HrMocks(human);
            hr.Contracts.Setup(r => r.GetEffectiveAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((HumanContract?)null);

            var payrolls = new Mock<IPayrollRepository>();

            var result = await hr.PayrollHandler(payrolls)
                .Handle(PayrollCommand(human.Id), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("NoEffectiveContract", result.Error.Code);
        }

        /// <summary>
        /// A salary that can be typed into the run is a salary that can be typed wrongly, and
        /// payroll is where a wrong number leaves the building before anybody notices.
        /// </summary>
        [Fact]
        public async Task The_salary_comes_from_the_contract_not_the_request()
        {
            var human = CreateHuman();
            var contract = new HumanContract(human, CreatePosition(), HumanEmploymentType.FullTime,
                DateRange.Create(new DateOnly(2026, 1, 1), null));
            contract.SetBaseSalary(Money.Create(77_000_000m, "IRR"));

            var hr = new HrMocks(human);
            hr.Contracts.Setup(r => r.GetEffectiveAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(contract);

            var payrolls = new Mock<IPayrollRepository>();

            var result = await hr.PayrollHandler(payrolls)
                .Handle(PayrollCommand(human.Id), Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(77_000_000m, result.Value.BaseSalary);

            // The base salary is a line like any other, so gross pay is the sum of the lines and
            // never a figure computed twice in two places.
            Assert.Equal(77_000_000m, result.Value.GrossPay);
            Assert.Contains(result.Value.Lines, l => l.Code == "BASE");
        }
    }
}
