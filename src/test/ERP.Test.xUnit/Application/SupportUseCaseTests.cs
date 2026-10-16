using ERP.Application.Common.Interfaces.Identity;
using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Commands.CloseTicket;
using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Commands.OpenTicket;
using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Commands.ReplyToTicket;
using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Commands.ResolveTicket;
using ERP.Domain.Entities.Common;
using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;
using ERP.Domain.Repositories.Common;
using ERP.Domain.Repositories.Modules.CustomersRelationshipManagement;
using Moq;

namespace ERP.Test.xUnit.Application
{
    /// <summary>
    /// The support desk. What is actually being tested is the clock: a ticket without a promised
    /// response time is a note, and one with a promise is something the desk can be held to.
    /// </summary>
    public sealed class SupportUseCaseTests
    {
        private static readonly DateTimeOffset Now = new(2026, 5, 10, 8, 0, 0, TimeSpan.Zero);

        private static CancellationToken Token => TestContext.Current.CancellationToken;

        private sealed class FixedClock(DateTimeOffset now) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => now;
        }

        private sealed class StubUser(Guid? userId, string? userName) : ICurrentUser
        {
            public Guid? UserId => userId;
            public string? UserName => userName;
            public bool IsAuthenticated => userId is not null;
        }

        private static Customer CreateCustomer() => new("C-1", "Sepehr Trading", CustomerType.Organization);

        private static TicketSubject CreateSubject(bool active = true, (int Response, int Resolution)? serviceLevel = null)
        {
            var subject = new TicketSubject("Delivery");

            if (serviceLevel is not null)
                subject.SetServiceLevel(serviceLevel.Value.Response, serviceLevel.Value.Resolution);

            if (!active)
                subject.Deactivate();

            return subject;
        }

        private sealed class OpenHarness
        {
            public Mock<ICustomerRepository> Customers { get; } = new();
            public Mock<ITicketSubjectRepository> Subjects { get; } = new();
            public Mock<ITicketRepository> Tickets { get; } = new();
            public Mock<INumberingSeriesRepository> Series { get; } = new();
            public Ticket? Created { get; private set; }

            public OpenHarness(Customer? customer, TicketSubject? subject)
            {
                Customers.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(customer);

                Subjects.Setup(r => r.GetWithAncestorsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(subject);

                Series.Setup(r => r.GetForUpdateAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new NumberingSeries("Ticket", paddingLength: 5));

                Tickets.Setup(r => r.CreateAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()))
                    .Callback<Ticket, CancellationToken>((ticket, _) => Created = ticket)
                    .Returns(Task.CompletedTask);
            }

            public OpenTicketCommandHandler Build() => new(
                Customers.Object, Subjects.Object, Tickets.Object, Series.Object, new FixedClock(Now));
        }

        private static OpenTicketCommand OpenCommand(Guid customerId, Guid subjectId, TicketPriority? priority = null) => new()
        {
            CustomerId = customerId,
            SubjectId = subjectId,
            Title = "Order arrived damaged",
            Message = "Two of the four boxes were crushed.",
            Priority = priority
        };

        [Fact]
        public async Task The_promised_times_come_from_the_subject()
        {
            var customer = CreateCustomer();
            var subject = CreateSubject(serviceLevel: (60, 480));
            var harness = new OpenHarness(customer, subject);

            var result = await harness.Build().Handle(OpenCommand(customer.Id, subject.Id), Token);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value.ResponseDueAt);
            Assert.NotNull(result.Value.ResolutionDueAt);

            // Measured from when the ticket was created, not from now, so a ticket entered late
            // does not get a longer clock than the one the customer was promised.
            Assert.Equal(420, (result.Value.ResolutionDueAt!.Value - result.Value.ResponseDueAt!.Value).TotalMinutes);
        }

        [Fact]
        public async Task A_subject_that_promises_nothing_leaves_the_ticket_without_a_due_time()
        {
            // Better than inventing a promise nobody made: a missing due time reads as unpromised,
            // an invented one reads as met or breached against a target that does not exist.
            var customer = CreateCustomer();
            var subject = CreateSubject();
            var harness = new OpenHarness(customer, subject);

            var result = await harness.Build().Handle(OpenCommand(customer.Id, subject.Id), Token);

            Assert.True(result.IsSuccess);
            Assert.Null(result.Value.ResponseDueAt);
            Assert.Null(result.Value.ResolutionDueAt);
        }

        [Fact]
        public async Task A_subject_inherits_the_promise_from_its_parent()
        {
            var customer = CreateCustomer();
            var parent = CreateSubject(serviceLevel: (30, 240));
            var child = new TicketSubject("Damaged in transit", parent);

            var harness = new OpenHarness(customer, child);

            var result = await harness.Build().Handle(OpenCommand(customer.Id, child.Id), Token);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value.ResponseDueAt);
        }

        [Fact]
        public async Task A_retired_subject_takes_no_new_tickets()
        {
            var customer = CreateCustomer();
            var subject = CreateSubject(active: false);
            var harness = new OpenHarness(customer, subject);

            var result = await harness.Build().Handle(OpenCommand(customer.Id, subject.Id), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("TicketSubjectIsInactive", result.Error.Code);
        }

        [Fact]
        public async Task The_first_message_is_the_customers_and_does_not_stop_the_response_clock()
        {
            var customer = CreateCustomer();
            var subject = CreateSubject(serviceLevel: (60, 480));
            var harness = new OpenHarness(customer, subject);

            var result = await harness.Build().Handle(OpenCommand(customer.Id, subject.Id), Token);

            Assert.True(result.IsSuccess);
            Assert.Null(result.Value.FirstRespondedAt);

            var message = Assert.Single(result.Value.Messages);
            Assert.Equal(TicketMessageCreatorType.Customer, message.CreatorType);
            Assert.Equal(customer.Id.ToString(), message.CreatorId);
        }

        [Fact]
        public async Task The_subjects_default_priority_applies_when_the_request_names_none()
        {
            var customer = CreateCustomer();
            var subject = CreateSubject();
            subject.SetDefaults(TicketPriority.High, null);

            var harness = new OpenHarness(customer, subject);

            var result = await harness.Build().Handle(OpenCommand(customer.Id, subject.Id), Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(TicketPriority.High, result.Value.Priority);
        }

        private static Ticket CreateTicket(TicketStatus status = TicketStatus.Open)
        {
            var ticket = new Ticket("T-1", CreateCustomer(), CreateSubject(), "Order arrived damaged");
            ticket.AddMessage(new TicketMessage(ticket, TicketMessageCreatorType.Customer, ticket.CustomerId.ToString(), "Two boxes were crushed."));

            if (status is TicketStatus.Resolved or TicketStatus.Closed)
                ticket.Resolve("Replacements sent.");

            if (status == TicketStatus.Closed)
                ticket.Close();

            return ticket;
        }

        private static Mock<ITicketRepository> TicketRepository(Ticket? ticket)
        {
            var repository = new Mock<ITicketRepository>();
            repository.Setup(r => r.GetWithMessagesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
            return repository;
        }

        [Fact]
        public async Task A_reply_from_the_desk_stops_the_response_clock()
        {
            var ticket = CreateTicket();
            var handler = new ReplyToTicketCommandHandler(
                TicketRepository(ticket).Object,
                new StubUser(Guid.NewGuid(), "hasti"));

            var result = await handler.Handle(
                new ReplyToTicketCommand { TicketId = ticket.Id, Message = "Replacements are on the way." },
                Token);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value.FirstRespondedAt);
        }

        [Fact]
        public async Task A_message_cannot_be_attributed_to_somebody_else()
        {
            // The request says which side is speaking; who they are is taken from the caller. A
            // desk reply that could name any author is a desk reply nobody can be held to.
            var ticket = CreateTicket();
            var userId = Guid.NewGuid();

            var handler = new ReplyToTicketCommandHandler(TicketRepository(ticket).Object, new StubUser(userId, "hasti"));

            var result = await handler.Handle(
                new ReplyToTicketCommand { TicketId = ticket.Id, Message = "Looking into it." },
                Token);

            Assert.True(result.IsSuccess);

            var reply = result.Value.Messages.Last();
            Assert.Equal(TicketMessageCreatorType.Human, reply.CreatorType);
            Assert.Equal(userId.ToString(), reply.CreatorId);
        }

        [Fact]
        public async Task A_reply_that_asks_a_question_can_put_the_ticket_on_the_customer()
        {
            var ticket = CreateTicket();
            var handler = new ReplyToTicketCommandHandler(TicketRepository(ticket).Object, new StubUser(Guid.NewGuid(), "hasti"));

            var result = await handler.Handle(
                new ReplyToTicketCommand { TicketId = ticket.Id, Message = "Can you send a photo?", AwaitCustomer = true },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(TicketStatus.AwaitingCustomer, result.Value.Status);
        }

        [Fact]
        public async Task A_customers_own_message_never_puts_the_ticket_back_on_them()
        {
            var ticket = CreateTicket();
            var handler = new ReplyToTicketCommandHandler(TicketRepository(ticket).Object, new StubUser(Guid.NewGuid(), "hasti"));

            var result = await handler.Handle(
                new ReplyToTicketCommand
                {
                    TicketId = ticket.Id,
                    Message = "Here is the photo.",
                    CreatorType = TicketMessageCreatorType.Customer,
                    AwaitCustomer = true
                },
                Token);

            Assert.True(result.IsSuccess);
            Assert.NotEqual(TicketStatus.AwaitingCustomer, result.Value.Status);
        }

        [Fact]
        public async Task A_closed_ticket_takes_no_new_messages()
        {
            var ticket = CreateTicket(TicketStatus.Closed);
            var handler = new ReplyToTicketCommandHandler(TicketRepository(ticket).Object, new StubUser(Guid.NewGuid(), "hasti"));

            var result = await handler.Handle(
                new ReplyToTicketCommand { TicketId = ticket.Id, Message = "One more thing." },
                Token);

            Assert.True(result.IsFailure);
            Assert.Equal("TicketCannotBeChanged", result.Error.Code);
        }

        [Fact]
        public async Task Resolving_and_closing_are_separate_unless_the_desk_says_otherwise()
        {
            var ticket = CreateTicket();
            var handler = new ResolveTicketCommandHandler(TicketRepository(ticket).Object);

            var result = await handler.Handle(
                new ResolveTicketCommand { TicketId = ticket.Id, Resolution = "Replacements sent." },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(TicketStatus.Resolved, result.Value.Status);
            Assert.Null(result.Value.ClosedAt);
        }

        [Fact]
        public async Task A_desk_that_does_not_wait_for_the_customer_can_close_in_the_same_step()
        {
            var ticket = CreateTicket();
            var handler = new ResolveTicketCommandHandler(TicketRepository(ticket).Object);

            var result = await handler.Handle(
                new ResolveTicketCommand { TicketId = ticket.Id, Resolution = "Replacements sent.", CloseImmediately = true },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(TicketStatus.Closed, result.Value.Status);
        }

        [Fact]
        public async Task A_ticket_nobody_has_resolved_cannot_be_closed()
        {
            var ticket = CreateTicket();
            var handler = new CloseTicketCommandHandler(TicketRepository(ticket).Object);

            var result = await handler.Handle(new CloseTicketCommand { TicketId = ticket.Id }, Token);

            Assert.True(result.IsFailure);
            Assert.Equal("TicketCannotBeChanged", result.Error.Code);
        }

        [Fact]
        public async Task A_rating_is_recorded_on_the_close()
        {
            var ticket = CreateTicket(TicketStatus.Resolved);
            var handler = new CloseTicketCommandHandler(TicketRepository(ticket).Object);

            var result = await handler.Handle(
                new CloseTicketCommand { TicketId = ticket.Id, SatisfactionRating = 4 },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(TicketStatus.Closed, result.Value.Status);
            Assert.Equal(4, result.Value.SatisfactionRating);
        }

        [Fact]
        public async Task A_ticket_that_does_not_exist_is_a_not_found_rather_than_a_crash()
        {
            var handler = new CloseTicketCommandHandler(TicketRepository(null).Object);

            var result = await handler.Handle(new CloseTicketCommand { TicketId = Guid.NewGuid() }, Token);

            Assert.True(result.IsFailure);
            Assert.Equal("RecordNotFound", result.Error.Code);
        }
    }
}
