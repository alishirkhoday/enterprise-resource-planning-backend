using ERP.Application.UseCases.Modules.CorrespondenceManagement.Commands.AnswerReferral;
using ERP.Application.UseCases.Modules.CorrespondenceManagement.Commands.ReferLetter;
using ERP.Application.UseCases.Modules.CorrespondenceManagement.Commands.RegisterLetter;
using ERP.Application.UseCases.Modules.CorrespondenceManagement.DTOs;
using ERP.Domain.Entities.Common;
using ERP.Domain.Entities.Modules.CorrespondenceManagement;
using ERP.Domain.Repositories.Common;
using ERP.Domain.Repositories.Modules.CorrespondenceManagement;
using Moq;

namespace ERP.Test.xUnit.Application
{
    /// <summary>
    /// The register: numbers that stay citable, and hand-offs that can be accounted for.
    /// </summary>
    public sealed class CorrespondenceUseCaseTests
    {
        private static readonly DateTimeOffset Now = new(2026, 9, 20, 9, 0, 0, TimeSpan.Zero);

        private static CancellationToken Token => TestContext.Current.CancellationToken;

        private sealed class FixedClock(DateTimeOffset now) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => now;
        }

        private sealed class RegisterHarness
        {
            public Mock<ILetterRepository> Letters { get; } = new();
            public Mock<INumberingSeriesRepository> Series { get; } = new();
            public List<string> SeriesKeysAsked { get; } = [];
            public Letter? Created { get; private set; }

            public RegisterHarness(Letter? duplicate = null, NumberingSeries? series = null)
            {
                Letters.Setup(r => r.GetByNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(duplicate);

                Series.Setup(r => r.GetForUpdateAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .Callback<string, Guid?, Guid?, CancellationToken>((key, _, _, _) => SeriesKeysAsked.Add(key))
                    .ReturnsAsync(series ?? new NumberingSeries("Letter", paddingLength: 4));

                Letters.Setup(r => r.CreateAsync(It.IsAny<Letter>(), It.IsAny<CancellationToken>()))
                    .Callback<Letter, CancellationToken>((letter, _) => Created = letter)
                    .Returns(Task.CompletedTask);
            }

            public RegisterLetterCommandHandler Build() => new(Letters.Object, Series.Object, new FixedClock(Now));
        }

        private static RegisterLetterCommand RegisterCommand(LetterDirection direction) => new()
        {
            Direction = direction,
            Subject = "Tax assessment",
            CorrespondentName = "Tax Authority",
            LetterDate = new DateOnly(2026, 9, 18)
        };

        /// <summary>
        /// The two books are kept apart so a number always says which way the letter went.
        /// </summary>
        [Theory]
        [InlineData(LetterDirection.Inbound, "Letter.Inbound")]
        [InlineData(LetterDirection.Outbound, "Letter.Outbound")]
        [InlineData(LetterDirection.Internal, "Letter.Internal")]
        public async Task Each_direction_draws_from_its_own_sequence(LetterDirection direction, string expectedKey)
        {
            var harness = new RegisterHarness();

            await harness.Build().Handle(RegisterCommand(direction), Token);

            Assert.Contains(expectedKey, harness.SeriesKeysAsked);
        }

        [Fact]
        public async Task A_registered_letter_carries_its_number()
        {
            var harness = new RegisterHarness(series: new NumberingSeries("Letter", prefix: "1405/", paddingLength: 4));

            var result = await harness.Build().Handle(RegisterCommand(LetterDirection.Inbound), Token);

            Assert.True(result.IsSuccess);
            Assert.StartsWith("1405/", result.Value.Number, StringComparison.Ordinal);
            Assert.Equal(LetterStatus.Registered, result.Value.Status);
        }

        /// <summary>
        /// A number already in the register means somebody else entered this letter, and issuing
        /// it twice would break the citation that makes the register worth keeping.
        /// </summary>
        [Fact]
        public async Task A_number_already_in_the_register_is_refused()
        {
            var existing = new Letter("1405/0001", LetterDirection.Inbound, "Something else", "Ministry", new DateOnly(2026, 9, 1));
            var harness = new RegisterHarness(duplicate: existing);

            var result = await harness.Build().Handle(RegisterCommand(LetterDirection.Inbound), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("LetterNumberIsUsed", result.Error.Code);
        }

        [Fact]
        public async Task An_outgoing_letter_records_its_addressees()
        {
            var harness = new RegisterHarness();

            var command = RegisterCommand(LetterDirection.Outbound) with
            {
                Recipients =
                [
                    new LetterRecipientDto { Kind = LetterRecipientKind.Primary, Name = "Tax Authority" },
                    new LetterRecipientDto { Kind = LetterRecipientKind.Copy, Name = "Finance Director" }
                ]
            };

            var result = await harness.Build().Handle(command, Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(2, harness.Created!.Recipients.Count);
        }

        private static Letter CreateRegisteredLetter()
        {
            var letter = new Letter("1405/0001", LetterDirection.Inbound, "Tax assessment", "Tax Authority", new DateOnly(2026, 9, 18));
            return letter;
        }

        private static Mock<ILetterRepository> LettersReturning(Letter letter)
        {
            var letters = new Mock<ILetterRepository>();
            letters.Setup(r => r.GetWithRecipientsAndReferralsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(letter);
            letters.Setup(r => r.GetByReferralAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(letter);
            return letters;
        }

        [Fact]
        public async Task Referring_a_letter_puts_it_in_somebody_queue()
        {
            var letter = CreateRegisteredLetter();
            var toUserId = Guid.NewGuid();
            var letters = LettersReturning(letter);

            var result = await new ReferLetterCommandHandler(letters.Object).Handle(
                new ReferLetterCommand
                {
                    LetterId = letter.Id,
                    ToUserId = toUserId,
                    FromUserId = Guid.NewGuid(),
                    Instruction = "Please reply within a week."
                },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(LetterStatus.InProgress, result.Value.Status);
            Assert.Equal(1, result.Value.OutstandingReferralCount);
        }

        [Fact]
        public async Task The_same_person_cannot_be_given_the_same_letter_twice()
        {
            var letter = CreateRegisteredLetter();
            var toUserId = Guid.NewGuid();
            letter.ReferTo(toUserId, Guid.NewGuid());

            var letters = LettersReturning(letter);

            var result = await new ReferLetterCommandHandler(letters.Object).Handle(
                new ReferLetterCommand { LetterId = letter.Id, ToUserId = toUserId, FromUserId = Guid.NewGuid() },
                Token);

            Assert.True(result.IsFailure);
            Assert.Equal("LetterCannotBeReferred", result.Error.Code);
        }

        [Fact]
        public async Task An_archived_letter_cannot_be_referred()
        {
            var letter = CreateRegisteredLetter();
            letter.Archive("Cabinet 4");

            var letters = LettersReturning(letter);

            var result = await new ReferLetterCommandHandler(letters.Object).Handle(
                new ReferLetterCommand { LetterId = letter.Id, ToUserId = Guid.NewGuid(), FromUserId = Guid.NewGuid() },
                Token);

            Assert.True(result.IsFailure);
            Assert.Equal("LetterCannotBeReferred", result.Error.Code);
        }

        /// <summary>
        /// Once nobody is holding it the letter is done, so it files itself rather than waiting
        /// for somebody to remember.
        /// </summary>
        [Fact]
        public async Task Answering_the_last_referral_closes_the_letter()
        {
            var letter = CreateRegisteredLetter();
            var referral = letter.ReferTo(Guid.NewGuid(), Guid.NewGuid());

            var letters = LettersReturning(letter);

            var result = await new AnswerReferralCommandHandler(letters.Object).Handle(
                new AnswerReferralCommand { ReferralId = referral.Id, Response = "Answered by letter 1405/1300." },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(LetterStatus.Closed, result.Value.Status);
            Assert.Equal(0, result.Value.OutstandingReferralCount);
        }

        [Fact]
        public async Task A_letter_somebody_is_still_holding_stays_open()
        {
            var letter = CreateRegisteredLetter();
            var first = letter.ReferTo(Guid.NewGuid(), Guid.NewGuid());
            letter.ReferTo(Guid.NewGuid(), Guid.NewGuid());

            var letters = LettersReturning(letter);

            var result = await new AnswerReferralCommandHandler(letters.Object).Handle(
                new AnswerReferralCommand { ReferralId = first.Id, Response = "Passed on." },
                Token);

            Assert.Equal(LetterStatus.InProgress, result.Value.Status);
            Assert.Equal(1, result.Value.OutstandingReferralCount);
        }

        /// <summary>
        /// A withdrawal closes the same open item as an answer, which is why both go through one
        /// command - something has to account for every referral that was ever made.
        /// </summary>
        [Fact]
        public async Task Taking_a_referral_back_also_settles_it()
        {
            var letter = CreateRegisteredLetter();
            var referral = letter.ReferTo(Guid.NewGuid(), Guid.NewGuid());

            var letters = LettersReturning(letter);

            var result = await new AnswerReferralCommandHandler(letters.Object).Handle(
                new AnswerReferralCommand { ReferralId = referral.Id, Response = "Sent to the wrong desk.", IsWithdrawal = true },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(LetterReferralStatus.Withdrawn, referral.Status);
            Assert.Equal(LetterStatus.Closed, result.Value.Status);
        }

        [Fact]
        public async Task A_referral_already_answered_cannot_be_answered_again()
        {
            var letter = CreateRegisteredLetter();
            var referral = letter.ReferTo(Guid.NewGuid(), Guid.NewGuid());
            referral.Letter.RespondLetterReferral(referral.Id, "Done.");

            var letters = LettersReturning(letter);

            var result = await new AnswerReferralCommandHandler(letters.Object).Handle(
                new AnswerReferralCommand { ReferralId = referral.Id, Response = "Done again." },
                Token);

            Assert.True(result.IsFailure);
            Assert.Equal("ReferralCannotBeAnswered", result.Error.Code);
        }
    }
}
