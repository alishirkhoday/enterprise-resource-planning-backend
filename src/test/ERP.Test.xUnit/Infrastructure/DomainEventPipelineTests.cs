using ERP.Application.Common.Interfaces.Events;
using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;
using ERP.Domain.Entities.Notifications;
using ERP.Domain.Entities.Users;
using ERP.Infrastructure.MainDatabase.Context;
using ERP.Infrastructure.MainDatabase.Repositories.Notifications;
using ERP.Infrastructure.Services.Events;
using Microsoft.EntityFrameworkCore;

namespace ERP.Test.xUnit.Infrastructure
{
    /// <summary>
    /// A domain event is only worth raising if it survives the transaction that produced it and
    /// reaches a handler exactly once. These tests follow one fact the whole way: recorded by an
    /// entity, written into the outbox alongside the change, and dispatched afterwards.
    /// </summary>
    public sealed class DomainEventPipelineTests
    {
        private sealed class RecordingDispatcher : IDomainEventDispatcher
        {
            public List<IDomainEvent> Dispatched { get; } = [];
            public Func<IDomainEvent, bool>? Fail { get; set; }

            public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
            {
                if (Fail?.Invoke(domainEvent) == true)
                    throw new InvalidOperationException("The handler refused it.");

                Dispatched.Add(domainEvent);
                return Task.CompletedTask;
            }

            public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
            {
                foreach (var domainEvent in domainEvents)
                {
                    await DispatchAsync(domainEvent, cancellationToken);
                }
            }
        }

        private static MainDbContext CreateContext(string name) =>
            new(new DbContextOptionsBuilder<MainDbContext>().UseInMemoryDatabase(name).Options,
                UnrestrictedBranchContext.Instance);

        private static CancellationToken Token => TestContext.Current.CancellationToken;

        [Fact]
        public async Task Saving_a_change_writes_the_facts_it_produced_into_the_outbox()
        {
            await using var context = CreateContext(nameof(Saving_a_change_writes_the_facts_it_produced_into_the_outbox));
            var user = new User("kaveh.ahangar", "hash");

            context.Users.Add(user);
            await context.SaveChanges(Token);

            var written = await context.OutboxMessages.ToListAsync(Token);
            Assert.Contains(written, m => m.MessageType == nameof(UserRegistered));
        }

        /// <summary>
        /// Cleared only after the write succeeded, so a save that throws leaves the facts on the
        /// entity for the caller to retry with.
        /// </summary>
        [Fact]
        public async Task The_events_are_cleared_from_the_entity_once_they_are_safely_stored()
        {
            await using var context = CreateContext(nameof(The_events_are_cleared_from_the_entity_once_they_are_safely_stored));
            var user = new User("kaveh.ahangar", "hash");

            context.Users.Add(user);
            await context.SaveChanges(Token);

            Assert.Empty(user.DomainEvents);
        }

        [Fact]
        public async Task A_second_save_does_not_record_the_same_fact_again()
        {
            await using var context = CreateContext(nameof(A_second_save_does_not_record_the_same_fact_again));
            var user = new User("kaveh.ahangar", "hash");

            context.Users.Add(user);
            await context.SaveChanges(Token);
            user.ConfirmEmail();
            await context.SaveChanges(Token);

            var registrations = await context.OutboxMessages.CountAsync(m => m.MessageType == nameof(UserRegistered), Token);
            Assert.Equal(1, registrations);
        }

        [Fact]
        public async Task A_change_that_produced_no_facts_writes_nothing()
        {
            await using var context = CreateContext(nameof(A_change_that_produced_no_facts_writes_nothing));

            context.CustomerGroups.Add(new CustomerGroup("VIP", "Key accounts"));
            await context.SaveChanges(Token);

            Assert.Empty(await context.OutboxMessages.ToListAsync(Token));
        }

        /// <summary>
        /// The identity and the moment have to survive storage. They are what a consumer uses to
        /// tell a redelivery from a genuine second occurrence, and a fresh pair on the way back
        /// would make every repeat look new.
        /// </summary>
        [Fact]
        public void A_stored_fact_comes_back_as_the_same_fact()
        {
            var original = new ChequeBounced(Guid.NewGuid(), "123456", "Insufficient funds.");

            var restored = DomainEventSerializer.Deserialize(
                DomainEventSerializer.TypeNameOf(original),
                DomainEventSerializer.Serialize(original));

            var bounced = Assert.IsType<ChequeBounced>(restored);
            Assert.Equal(original.EventId, bounced.EventId);
            Assert.Equal(original.OccurredOn, bounced.OccurredOn);
            Assert.Equal(original, bounced);
        }

        [Fact]
        public void An_unknown_event_name_fails_loudly_rather_than_being_skipped()
        {
            Assert.Throws<InvalidOperationException>(() =>
                DomainEventSerializer.Deserialize("SomethingThatWasRenamed", "{}"));
        }

        [Fact]
        public void Every_event_the_domain_defines_can_be_resolved_by_name()
        {
            var declared = typeof(IDomainEvent).Assembly
                .GetTypes()
                .Count(t => t is { IsClass: true, IsAbstract: false } && t.IsAssignableTo(typeof(IDomainEvent)));

            Assert.Equal(declared, DomainEventSerializer.KnownEventNames.Count);
        }

        [Fact]
        public async Task The_processor_dispatches_what_the_outbox_holds_and_marks_it_done()
        {
            await using var context = CreateContext(nameof(The_processor_dispatches_what_the_outbox_holds_and_marks_it_done));
            context.Users.Add(new User("kaveh.ahangar", "hash"));
            await context.SaveChanges(Token);

            var dispatcher = new RecordingDispatcher();
            var processed = await new OutboxProcessor(new OutboxMessageRepository(context), dispatcher, context)
                .ProcessPendingAsync(cancellationToken: Token);

            Assert.Equal(1, processed);
            Assert.Contains(dispatcher.Dispatched, e => e is UserRegistered);
            Assert.All(await context.OutboxMessages.ToListAsync(Token),
                m => Assert.Equal(OutboxMessageStatus.Processed, m.Status));
        }

        [Fact]
        public async Task A_message_already_processed_is_not_dispatched_again()
        {
            await using var context = CreateContext(nameof(A_message_already_processed_is_not_dispatched_again));
            context.Users.Add(new User("kaveh.ahangar", "hash"));
            await context.SaveChanges(Token);

            var dispatcher = new RecordingDispatcher();
            var processor = new OutboxProcessor(new OutboxMessageRepository(context), dispatcher, context);

            await processor.ProcessPendingAsync(cancellationToken: Token);
            var second = await processor.ProcessPendingAsync(cancellationToken: Token);

            Assert.Equal(0, second);
            Assert.Single(dispatcher.Dispatched);
        }

        /// <summary>
        /// A handler that throws must not lose the fact. The message goes back to pending with a
        /// back-off, so the next pass picks it up instead of the work disappearing.
        /// </summary>
        [Fact]
        public async Task A_failing_handler_leaves_the_fact_to_be_retried()
        {
            await using var context = CreateContext(nameof(A_failing_handler_leaves_the_fact_to_be_retried));
            context.Users.Add(new User("kaveh.ahangar", "hash"));
            await context.SaveChanges(Token);

            var dispatcher = new RecordingDispatcher { Fail = _ => true };
            var processed = await new OutboxProcessor(new OutboxMessageRepository(context), dispatcher, context)
                .ProcessPendingAsync(cancellationToken: Token);

            var message = await context.OutboxMessages.SingleAsync(Token);

            Assert.Equal(0, processed);
            Assert.Equal(OutboxMessageStatus.Pending, message.Status);
            Assert.Equal(1, message.AttemptCount);
            Assert.NotNull(message.NextAttemptOn);
        }

        /// <summary>
        /// One poisonous payload backing off on its own schedule must not hold up the queue behind
        /// it, so each message is judged on its own.
        /// </summary>
        [Fact]
        public async Task One_bad_message_does_not_block_the_rest_of_the_batch()
        {
            await using var context = CreateContext(nameof(One_bad_message_does_not_block_the_rest_of_the_batch));
            context.Users.Add(new User("kaveh.ahangar", "hash"));
            context.Users.Add(new User("sara.esfandiari", "hash"));
            await context.SaveChanges(Token);

            var firstSeen = false;
            var dispatcher = new RecordingDispatcher
            {
                Fail = _ =>
                {
                    if (firstSeen)
                        return false;
                    firstSeen = true;
                    return true;
                }
            };

            var processed = await new OutboxProcessor(new OutboxMessageRepository(context), dispatcher, context)
                .ProcessPendingAsync(cancellationToken: Token);

            Assert.Equal(1, processed);
            Assert.Single(dispatcher.Dispatched);
            Assert.Equal(1, await context.OutboxMessages.CountAsync(m => m.Status == OutboxMessageStatus.Pending, Token));
        }
    }
}
