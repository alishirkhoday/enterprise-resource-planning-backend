using ERP.Domain.Entities.Modules.FinanceManagement;

namespace ERP.Test.xUnit.Domain
{
    /// <summary>
    /// A cheque moves through states in one direction only; each move is an accounting event, so an
    /// illegal jump has to be refused rather than silently allowed.
    /// </summary>
    public sealed class ChequeLifecycleTests
    {
        private static Cheque CreateReceivedCheque() =>
            new(ChequeType.Received, "123456", Money.Create(50_000_000m, "IRR"),
                new DateOnly(2025, 4, 1), new DateOnly(2025, 6, 1));

        [Fact]
        public void A_received_cheque_starts_in_hand()
        {
            Assert.Equal(ChequeStatus.InHand, CreateReceivedCheque().Status);
        }

        [Fact]
        public void Depositing_then_clearing_settles_the_cheque()
        {
            var cheque = CreateReceivedCheque();

            cheque.Deposit(Guid.NewGuid(), new DateOnly(2025, 5, 28));
            Assert.Equal(ChequeStatus.InCollection, cheque.Status);

            cheque.Clear(new DateOnly(2025, 6, 1));
            Assert.Equal(ChequeStatus.Cleared, cheque.Status);
            Assert.Equal(new DateOnly(2025, 6, 1), cheque.SettledOn);
        }

        [Fact]
        public void A_cheque_in_hand_cannot_clear_without_being_deposited()
        {
            var cheque = CreateReceivedCheque();

            Assert.Throws<InvalidOperationException>(() => cheque.Clear(new DateOnly(2025, 6, 1)));
        }

        [Fact]
        public void A_bounce_is_recorded_with_its_reason_and_raises_an_event()
        {
            var cheque = CreateReceivedCheque();
            cheque.Deposit(Guid.NewGuid(), new DateOnly(2025, 5, 28));

            cheque.Bounce(new DateOnly(2025, 6, 2), "Insufficient funds");

            Assert.Equal(ChequeStatus.Bounced, cheque.Status);
            Assert.Equal("Insufficient funds", cheque.BounceReason);
            Assert.Contains(cheque.DomainEvents, e => e is ChequeBounced);
        }

        [Fact]
        public void Every_step_is_kept_in_the_operation_history()
        {
            var cheque = CreateReceivedCheque();

            cheque.Deposit(Guid.NewGuid(), new DateOnly(2025, 5, 28));
            cheque.Clear(new DateOnly(2025, 6, 1));

            Assert.Equal(3, cheque.Operations.Count);
            Assert.Equal(ChequeOperationType.Registered, cheque.Operations[0].Type);
            Assert.Equal(ChequeOperationType.Deposited, cheque.Operations[1].Type);
            Assert.Equal(ChequeOperationType.Cleared, cheque.Operations[2].Type);
        }

        [Fact]
        public void An_issued_cheque_cannot_be_deposited()
        {
            var issued = new Cheque(ChequeType.Issued, "999", Money.Create(1_000m, "IRR"),
                new DateOnly(2025, 4, 1), new DateOnly(2025, 5, 1));

            Assert.Throws<InvalidOperationException>(() => issued.Deposit(Guid.NewGuid(), new DateOnly(2025, 4, 2)));
        }
    }
}
