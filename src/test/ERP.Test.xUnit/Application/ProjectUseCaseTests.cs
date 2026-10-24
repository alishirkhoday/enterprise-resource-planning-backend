using ERP.Application.UseCases.Modules.ProjectManagement.Commands.AchieveMilestone;
using ERP.Application.UseCases.Modules.ProjectManagement.Commands.DecideTimesheet;
using ERP.Application.UseCases.Modules.ProjectManagement.Commands.SubmitTimesheet;
using ERP.Application.UseCases.Modules.ProjectManagement.DTOs;
using ERP.Application.UseCases.Modules.ProjectManagement.Queries.GetProjectStatement;
using ERP.Domain.Entities.Modules.HumanResourcesManagement;
using ERP.Domain.Entities.Modules.ProjectManagement;
using ERP.Domain.Entities.Users;
using ERP.Domain.Repositories.Modules.HumanResourcesManagement;
using ERP.Domain.Repositories.Modules.ProjectManagement;
using Moq;

namespace ERP.Test.xUnit.Application
{
    /// <summary>
    /// What gets billed for a project, and what gates it. Only approved, task-bound hours reach a
    /// customer, and these hold the pipeline to that.
    /// </summary>
    public sealed class ProjectUseCaseTests
    {
        private static CancellationToken Token => TestContext.Current.CancellationToken;

        private static Human CreateHuman()
        {
            var human = new Human(new User("kaveh.ahangar", "hash"), "Kaveh", "Ahangar", HumanGender.Male, new DateOnly(1990, 5, 12), false);
            human.Hire(new DateOnly(2020, 1, 15));
            return human;
        }

        private static Project CreateProject() => new(
            "PRJ-1",
            "Warehouse rollout",
            ProjectType.Customer,
            DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)),
            "IRR");

        private static ProjectPhase CreatePhase(Project project)
        {
            var phase = new ProjectPhase(
            project, "Design", 1, DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31)), Percentage.Create(25m));
            project.AddPhase(phase);
            return phase;
        }

        private static ProjectTask CreateTask(ProjectPhase phase) => new(
            phase, "Draw the layout", DateRange.Create(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 7)), 40m);

        private sealed class TimesheetHarness
        {
            public Mock<IHumanRepository> Humans { get; } = new();
            public Mock<ITimesheetRepository> Timesheets { get; } = new();
            public Mock<IProjectRepository> Projects { get; } = new();
            public Timesheet? Created { get; private set; }

            public TimesheetHarness(Human human, ProjectTask? task, Timesheet? existing = null)
            {
                Humans.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(human);
                Projects.Setup(r => r.GetTaskAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(task);

                Timesheets.Setup(r => r.GetByHumanAndPeriodAsync(
                        It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(existing);

                Timesheets.Setup(r => r.CreateAsync(It.IsAny<Timesheet>(), It.IsAny<CancellationToken>()))
                    .Callback<Timesheet, CancellationToken>((sheet, _) => Created = sheet)
                    .Returns(Task.CompletedTask);
            }

            public SubmitTimesheetCommandHandler Build() => new(Humans.Object, Timesheets.Object, Projects.Object);
        }

        private static SubmitTimesheetCommand SubmitCommand(Guid humanId, params TimesheetEntryDto[] entries) => new()
        {
            HumanId = humanId,
            PeriodStart = new DateOnly(2026, 3, 2),
            PeriodEnd = new DateOnly(2026, 3, 6),
            Entries = entries
        };

        [Fact]
        public async Task A_submitted_timesheet_totals_its_hours()
        {
            var human = CreateHuman();
            var task = CreateTask(CreatePhase(CreateProject()));
            var harness = new TimesheetHarness(human, task);

            var result = await harness.Build().Handle(
                SubmitCommand(human.Id,
                    new TimesheetEntryDto { WorkDate = new DateOnly(2026, 3, 2), Hours = 8m, ProjectTaskId = task.Id },
                    new TimesheetEntryDto { WorkDate = new DateOnly(2026, 3, 3), Hours = 6m, ProjectTaskId = task.Id }),
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(14m, result.Value.TotalHours);
            Assert.Equal(TimesheetStatus.Submitted, result.Value.Status);
        }

        /// <summary>
        /// There is no customer to bill internal time to, so an entry with no task is never
        /// billable whatever the request asks for.
        /// </summary>
        [Fact]
        public async Task Time_with_no_task_is_never_billable()
        {
            var human = CreateHuman();
            var harness = new TimesheetHarness(human, null);

            var result = await harness.Build().Handle(
                SubmitCommand(human.Id, new TimesheetEntryDto
                {
                    WorkDate = new DateOnly(2026, 3, 2),
                    Hours = 8m,
                    ActivityType = TimesheetActivityType.Training,
                    IsBillable = true
                }),
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(0m, result.Value.BillableHours);
        }

        /// <summary>
        /// Somebody correcting Tuesday expects Tuesday corrected, not counted twice. A sheet that
        /// accumulates is how a week ends up with sixty billed hours in it.
        /// </summary>
        [Fact]
        public async Task Resubmitting_a_period_replaces_what_was_there()
        {
            var human = CreateHuman();
            var task = CreateTask(CreatePhase(CreateProject()));

            var existing = new Timesheet(human, DateRange.Create(new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 6)));
            existing.AddEntry(new TimesheetEntry(existing, new DateOnly(2026, 3, 2), 8m, task));

            var harness = new TimesheetHarness(human, task, existing);

            var result = await harness.Build().Handle(
                SubmitCommand(human.Id, new TimesheetEntryDto
                {
                    WorkDate = new DateOnly(2026, 3, 2),
                    Hours = 6m,
                    ProjectTaskId = task.Id
                }),
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(6m, result.Value.TotalHours);
            Assert.Single(result.Value.Entries);
        }

        [Fact]
        public async Task An_approved_period_cannot_be_rewritten()
        {
            var human = CreateHuman();
            var task = CreateTask(CreatePhase(CreateProject()));

            var existing = new Timesheet(human, DateRange.Create(new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 6)));
            existing.AddEntry(new TimesheetEntry(existing, new DateOnly(2026, 3, 2), 8m, task));
            existing.Submit();
            existing.Approve(Guid.NewGuid());

            var harness = new TimesheetHarness(human, task, existing);

            var result = await harness.Build().Handle(
                SubmitCommand(human.Id, new TimesheetEntryDto { WorkDate = new DateOnly(2026, 3, 2), Hours = 6m, ProjectTaskId = task.Id }),
                Token);

            Assert.True(result.IsFailure);
            Assert.Equal("TimesheetIsAlreadyApproved", result.Error.Code);
        }

        private static Timesheet CreateSubmittedTimesheet(Human human, ProjectTask task)
        {
            var sheet = new Timesheet(human, DateRange.Create(new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 6)));
            sheet.AddEntry(new TimesheetEntry(sheet, new DateOnly(2026, 3, 2), 8m, task));
            sheet.Submit();
            return sheet;
        }

        [Fact]
        public async Task Approving_a_timesheet_lets_its_hours_count()
        {
            var human = CreateHuman();
            var sheet = CreateSubmittedTimesheet(human, CreateTask(CreatePhase(CreateProject())));

            var timesheets = new Mock<ITimesheetRepository>();
            timesheets.Setup(r => r.GetWithEntriesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(sheet);

            var result = await new DecideTimesheetCommandHandler(timesheets.Object).Handle(
                new DecideTimesheetCommand { TimesheetId = sheet.Id, IsApproved = true, ApprovedByHumanId = Guid.NewGuid() },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(TimesheetStatus.Approved, result.Value.Status);
        }

        [Fact]
        public async Task A_timesheet_that_was_never_submitted_cannot_be_approved()
        {
            var human = CreateHuman();
            var sheet = new Timesheet(human, DateRange.Create(new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 6)));

            var timesheets = new Mock<ITimesheetRepository>();
            timesheets.Setup(r => r.GetWithEntriesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(sheet);

            var result = await new DecideTimesheetCommandHandler(timesheets.Object).Handle(
                new DecideTimesheetCommand { TimesheetId = sheet.Id, IsApproved = true, ApprovedByHumanId = Guid.NewGuid() },
                Token);

            Assert.True(result.IsFailure);
            Assert.Equal("TimesheetCannotBeDecided", result.Error.Code);
        }

        /// <summary>
        /// Whether a milestone counts as late is judged against its own due date, not asserted by
        /// whoever records it.
        /// </summary>
        [Theory]
        [InlineData("2026-03-18", MilestoneStatus.Achieved)]
        [InlineData("2026-03-25", MilestoneStatus.AchievedLate)]
        public async Task A_milestone_judges_its_own_lateness(string achievedOn, MilestoneStatus expected)
        {
            var phase = CreatePhase(CreateProject());
            var milestone = new ProjectMilestone(phase, "Design signed off", new DateOnly(2026, 3, 20));
            phase.Project.AddProjectPhaseMilestone(phase.Id, milestone);

            var projects = new Mock<IProjectRepository>();
            projects.Setup(r => r.GetMilestoneAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(milestone);

            var result = await new AchieveMilestoneCommandHandler(projects.Object).Handle(
                new AchieveMilestoneCommand { MilestoneId = milestone.Id, AchievedOn = DateOnly.Parse(achievedOn) },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(expected, result.Value.Status);
        }

        [Fact]
        public async Task A_milestone_cannot_be_achieved_twice()
        {
            var phase = CreatePhase(CreateProject());
            var milestone = new ProjectMilestone(phase, "Design signed off", new DateOnly(2026, 3, 20));
            phase.Project.AddProjectPhaseMilestone(phase.Id, milestone);
            milestone.Phase.Project.AchieveProjectMilestone(milestone.Id, new DateOnly(2026, 3, 18));

            var projects = new Mock<IProjectRepository>();
            projects.Setup(r => r.GetMilestoneAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(milestone);

            var result = await new AchieveMilestoneCommandHandler(projects.Object).Handle(
                new AchieveMilestoneCommand { MilestoneId = milestone.Id, AchievedOn = new DateOnly(2026, 3, 19) },
                Token);

            Assert.True(result.IsFailure);
            Assert.Equal("MilestoneCannotBeAchieved", result.Error.Code);
        }

        /// <summary>
        /// A statement reports approved hours only, and separates what is billable from what is
        /// not - the gap between them is where a fixed-price project loses money.
        /// </summary>
        [Fact]
        public async Task A_statement_reports_approved_hours_and_what_is_billable()
        {
            var project = CreateProject();
            var phase = CreatePhase(project);
            var milestone = new ProjectMilestone(phase, "Design signed off", new DateOnly(2026, 3, 20));
            phase.Project.AddProjectPhaseMilestone(phase.Id, milestone);
            milestone.Phase.Project.SetProjectMilestoneBillableAmount(milestone.Id, Money.Create(40_000_000m, "IRR"));
            milestone.Phase.Project.AchieveProjectMilestone(milestone.Id, new DateOnly(2026, 3, 18));

            var projects = new Mock<IProjectRepository>();
            projects.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(project);
            projects.Setup(r => r.GetBillableMilestonesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([milestone]);

            var timesheets = new Mock<ITimesheetRepository>();
            timesheets.Setup(r => r.GetApprovedHoursForProjectAsync(
                    It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(120m);
            timesheets.Setup(r => r.GetApprovedBillableHoursForProjectAsync(
                    It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(96m);

            var result = await new GetProjectStatementQueryHandler(projects.Object, timesheets.Object)
                .Handle(new GetProjectStatementQuery { ProjectId = project.Id }, Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(120m, result.Value.ApprovedHours);
            Assert.Equal(96m, result.Value.ApprovedBillableHours);
            Assert.Equal(40_000_000m, result.Value.BillableMilestoneValue);
            Assert.Single(result.Value.BillableMilestones);
        }

        /// <summary>
        /// Billable milestones come back for every project at once, so a statement has to keep
        /// only its own - otherwise one project reports another's revenue.
        /// </summary>
        [Fact]
        public async Task A_statement_counts_only_its_own_milestones()
        {
            var project = CreateProject();
            var otherProject = new Project("PRJ-2", "Something else", ProjectType.Internal,
                DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), "IRR");

            var otherMilestone = new ProjectMilestone(CreatePhase(otherProject), "Not ours", new DateOnly(2026, 3, 20));
            otherMilestone.Phase.Project.AddProjectPhaseMilestone(otherMilestone.Phase.Id, otherMilestone);
            otherMilestone.Phase.Project.SetProjectMilestoneBillableAmount(otherMilestone.Id, Money.Create(99_000_000m, "IRR"));
            otherMilestone.Phase.Project.AchieveProjectMilestone(otherMilestone.Id, new DateOnly(2026, 3, 18));

            var projects = new Mock<IProjectRepository>();
            projects.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(project);
            projects.Setup(r => r.GetBillableMilestonesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([otherMilestone]);

            var timesheets = new Mock<ITimesheetRepository>();

            var result = await new GetProjectStatementQueryHandler(projects.Object, timesheets.Object)
                .Handle(new GetProjectStatementQuery { ProjectId = project.Id }, Token);

            Assert.Empty(result.Value.BillableMilestones);
            Assert.Equal(0m, result.Value.BillableMilestoneValue);
        }
    }
}
