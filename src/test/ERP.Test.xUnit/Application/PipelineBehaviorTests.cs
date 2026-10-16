using CSharpFunctionalExtensions;
using ERP.Application.Common;
using ERP.Application.Common.Behaviors;
using ERP.Application.Common.Messaging;
using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;
using ERP.Infrastructure.MainDatabase.Context;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace ERP.Test.xUnit.Application
{
    /// <summary>
    /// The behaviours exist because every controller used to repeat the same three blocks by hand,
    /// and a controller that forgot one simply did without it. These tests hold the pipeline to
    /// doing what the repetition used to do - and to the two places it must deliberately not.
    /// </summary>
    public sealed class PipelineBehaviorTests
    {
        private sealed record TestCommand(string Name) : ICommand<Result<string, Error>>;

        private sealed record TestQuery(string Name) : IQuery<Result<string, Error>>;

        private sealed class TestCommandValidator : AbstractValidator<TestCommand>
        {
            public TestCommandValidator()
            {
                RuleFor(c => c.Name).NotEmpty().WithMessage("A name is required.");
                RuleFor(c => c.Name).MinimumLength(3).WithMessage("A name needs three characters.");
            }
        }

        private static CancellationToken Token => TestContext.Current.CancellationToken;

        private static ILogger Logger => new LoggerConfiguration().CreateLogger();

        private static MainDbContext CreateContext(string name) =>
            new(new DbContextOptionsBuilder<MainDbContext>().UseInMemoryDatabase(name).Options,
                UnrestrictedBranchContext.Instance);

        private static Task<Result<string, Error>> Succeeds() => Task.FromResult(Result.Success<string, Error>("done"));

        private static Task<Result<string, Error>> Refuses() =>
            Task.FromResult(Result.Failure<string, Error>(Errors.General.AnErrorHasOccurred()));

        [Fact]
        public async Task A_request_with_no_validator_passes_straight_through()
        {
            var behavior = new ValidationBehavior<TestQuery, Result<string, Error>>([]);

            var result = await behavior.Handle(new TestQuery(""), _ => Succeeds(), Token);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task An_invalid_request_never_reaches_its_handler()
        {
            var behavior = new ValidationBehavior<TestCommand, Result<string, Error>>([new TestCommandValidator()]);
            var handlerRan = false;

            await Assert.ThrowsAsync<ValidationException>(() =>
                behavior.Handle(
                    new TestCommand(""),
                    _ =>
                    {
                        handlerRan = true;
                        return Succeeds();
                    },
                    Token));

            Assert.False(handlerRan);
        }

        /// <summary>
        /// Every rule runs, so the caller is told about all of it at once instead of being sent
        /// round the loop one field at a time.
        /// </summary>
        [Fact]
        public async Task Every_broken_rule_is_reported_together()
        {
            var behavior = new ValidationBehavior<TestCommand, Result<string, Error>>([new TestCommandValidator()]);

            var exception = await Assert.ThrowsAsync<ValidationException>(() =>
                behavior.Handle(new TestCommand(""), _ => Succeeds(), Token));

            Assert.Equal(2, exception.Errors.Count());
        }

        [Fact]
        public async Task A_valid_request_reaches_its_handler()
        {
            var behavior = new ValidationBehavior<TestCommand, Result<string, Error>>([new TestCommandValidator()]);

            var result = await behavior.Handle(new TestCommand("Kaveh"), _ => Succeeds(), Token);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task A_successful_command_is_committed()
        {
            await using var context = CreateContext(nameof(A_successful_command_is_committed));
            var behavior = new UnitOfWorkBehavior<TestCommand, Result<string, Error>>(context);

            await behavior.Handle(
                new TestCommand("Kaveh"),
                _ =>
                {
                    context.CustomerGroups.Add(new CustomerGroup("VIP", "Key accounts"));
                    return Succeeds();
                },
                Token);

            Assert.Equal(1, await context.CustomerGroups.CountAsync(Token));
        }

        /// <summary>
        /// The important half. Several handlers reject partway through, after they have already put
        /// something in the change tracker - creating the user before discovering the national
        /// identifier is taken - and committing that would leave whichever half ran first.
        /// </summary>
        [Fact]
        public async Task A_refused_command_keeps_nothing()
        {
            await using var context = CreateContext(nameof(A_refused_command_keeps_nothing));
            var behavior = new UnitOfWorkBehavior<TestCommand, Result<string, Error>>(context);

            await behavior.Handle(
                new TestCommand("Kaveh"),
                _ =>
                {
                    context.CustomerGroups.Add(new CustomerGroup("VIP", "Key accounts"));
                    return Refuses();
                },
                Token);

            Assert.Equal(0, await context.CustomerGroups.CountAsync(Token));
        }

        [Fact]
        public async Task A_handler_that_threw_commits_nothing()
        {
            await using var context = CreateContext(nameof(A_handler_that_threw_commits_nothing));
            var behavior = new UnitOfWorkBehavior<TestCommand, Result<string, Error>>(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                behavior.Handle(
                    new TestCommand("Kaveh"),
                    _ =>
                    {
                        context.CustomerGroups.Add(new CustomerGroup("VIP", "Key accounts"));
                        throw new InvalidOperationException("The handler gave up.");
                    },
                    Token));

            Assert.Equal(0, await context.CustomerGroups.CountAsync(Token));
        }

        [Fact]
        public async Task Logging_lets_a_failure_through_unchanged()
        {
            var behavior = new LoggingBehavior<TestCommand, Result<string, Error>>(Logger);

            var result = await behavior.Handle(new TestCommand("Kaveh"), _ => Refuses(), Token);

            Assert.True(result.IsFailure);
        }

        [Fact]
        public async Task Logging_rethrows_rather_than_swallowing()
        {
            var behavior = new LoggingBehavior<TestCommand, Result<string, Error>>(Logger);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                behavior.Handle(
                    new TestCommand("Kaveh"),
                    _ => throw new InvalidOperationException("The handler gave up."),
                    Token));
        }

        /// <summary>
        /// A query cannot be handed to the unit of work at all: the behaviour is constrained to
        /// commands, so this is a compile-time guarantee being confirmed rather than a check.
        /// </summary>
        [Fact]
        public void A_query_is_not_a_command()
        {
            Assert.False(typeof(TestQuery).IsAssignableTo(typeof(ICommand<Result<string, Error>>)));
            Assert.True(typeof(TestCommand).IsAssignableTo(typeof(ICommand<Result<string, Error>>)));
        }
    }
}
